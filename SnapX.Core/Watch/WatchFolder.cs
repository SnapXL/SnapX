// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using SnapX.Core.Job;
using SnapX.Core.Utils;

namespace SnapX.Core.Watch;

public sealed class WatchFolder : IDisposable
{
    public WatchFolderSettings? Settings { get; set; }
    public TaskSettings? TaskSettings { get; set; } // Assuming this is for other task-related settings

    public delegate void FileWatcherTriggerEventHandler(string? path);
    public event FileWatcherTriggerEventHandler? FileWatcherTrigger;

    private SynchronizationContext _context;
    private FileSystemWatcher? _fileSystemWatcher;
    private List<WatchFolderDuplicateEventTimer> _timers = [];
    private PhysicalFileProvider? _fileProvider;

    /// <summary>
    /// Enables the file watcher.
    /// </summary>
    public void Enable()
    {
        Dispose();

        if (Settings == null)
        {
            DebugHelper.WriteLine("WatchFolder settings are not configured.");
            return;
        }

        var folderPath = FileHelpers.ExpandFolderVariables(Settings.FolderPath);

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            DebugHelper.WriteLine($"Invalid or non-existent folder path: {folderPath}");
            return;
        }

        _context = SynchronizationContext.Current ?? new SynchronizationContext();

        try
        {
            _fileSystemWatcher = new FileSystemWatcher(folderPath);
            if (!string.IsNullOrEmpty(Settings.Filter))
            {
                _fileSystemWatcher.Filter = Settings.Filter;
            }
            _fileSystemWatcher.IncludeSubdirectories = Settings.IncludeSubdirectories;

            _fileSystemWatcher.Created += FileWatcher_Created;
            _fileSystemWatcher.Changed += FileWatcher_Changed;
            _fileSystemWatcher.Renamed += FileWatcher_Renamed;
            _fileSystemWatcher.Deleted += FileWatcher_Deleted;

            _fileSystemWatcher.EnableRaisingEvents = true;
            DebugHelper.WriteLine($"Started monitoring directory: {folderPath} with filter: {_fileSystemWatcher.Filter}");

            // If you still want to initialize PhysicalFileProvider for other (non-event) purposes:
            _fileProvider = new PhysicalFileProvider(folderPath)
            {
                UsePollingFileWatcher = true,
                UseActivePolling = true
            };
            // Note: _fileProvider won't directly trigger the FileWatcherTrigger event.
            // You'd need to manually check for changes using IChangeToken if you wanted to use it for eventing.
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex, $"Error enabling file watcher for {folderPath}");
        }
    }

    /// <summary>
    /// Invokes the FileWatcherTrigger event on the synchronization context.
    /// </summary>
    /// <param name="path">The path of the file that triggered the event.</param>
    private void OnFileWatcherTrigger(string? path)
    {
        _context.Post(state => FileWatcherTrigger?.Invoke((string?)state), path);
    }

    /// <summary>
    /// Handles the FileSystemWatcher.Created event.
    /// </summary>
    private async void FileWatcher_Created(object sender, FileSystemEventArgs e)
    {
        DebugHelper.WriteLine($"File created event: {e.FullPath}");
        await HandleFileEvent(e.FullPath);
    }

    /// <summary>
    /// Handles the FileSystemWatcher.Changed event.
    /// </summary>
    private async void FileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        DebugHelper.WriteLine($"File changed event: {e.FullPath}");
        await HandleFileEvent(e.FullPath);
    }

    /// <summary>
    /// Handles the FileSystemWatcher.Renamed event.
    /// </summary>
    private async void FileWatcher_Renamed(object sender, RenamedEventArgs e)
    {
        DebugHelper.WriteLine($"File renamed event: Old path: {e.OldFullPath}, New path: {e.FullPath}");
        // For renamed, we typically care about the new path
        await HandleFileEvent(e.FullPath);
    }

    /// <summary>
    /// Handles the FileSystemWatcher.Deleted event.
    /// </summary>
    private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        DebugHelper.WriteLine($"File deleted event: {e.FullPath}");
        // No need to wait for file unlock/size for deleted files, just log or trigger if needed.
        // For deletion, you might have a separate event or just log.
        // If you need to trigger something for deleted files, consider a separate event or flag in FileWatcherTrigger.
    }


    /// <summary>
    /// Common handler for file system events, including debounce and file locking checks.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    private async Task HandleFileEvent(string path)
    {
        CleanElapsedTimers();

        // Skip directories for file processing
        if (Directory.Exists(path))
        {
            DebugHelper.WriteLine($"Skipping directory event for: {path}");
            return;
        }

        // Debounce duplicate events
        if (_timers.Any(timer => timer.IsDuplicateEvent(path)))
        {
            DebugHelper.WriteLine($"Skipping duplicate event for: {path}");
            return;
        }
        _timers.Add(new WatchFolderDuplicateEventTimer(path));

        var successCount = 0;
        long previousSize = -1;

        // Wait until the file is no longer locked and its size stabilizes
        await Helpers.WaitWhileAsync(
            check: () =>
            {
                if (!File.Exists(path))
                {
                    DebugHelper.WriteLine($"File {path} no longer exists during processing.");
                    return false;
                }

                if (!FileHelpers.IsFileLocked(path))
                {
                    var currentSize = FileHelpers.GetFileSize(path);

                    if (currentSize > 0 && currentSize == previousSize)
                    {
                        successCount++;
                    }
                    else
                    {
                        successCount = 0;
                    }

                    previousSize = currentSize;
                    return successCount < 4; // Wait for 4 consecutive stable size readings
                }

                previousSize = -1; // Reset previous size if file is locked
                return true;
            },
            interval: 250,
            timeout: 5000,
            waitStart: 0,
            onSuccess: () =>
            {
                OnFileWatcherTrigger(path);
            });
    }

    /// <summary>
    /// Cleans up elapsed duplicate event timers.
    /// </summary>
    private void CleanElapsedTimers()
    {
        _timers.RemoveAll(timer => timer.IsElapsed);
    }

    /// <summary>
    /// Disposes the FileSystemWatcher.
    /// </summary>
    public void Dispose()
    {
        if (_fileSystemWatcher != null)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Created -= FileWatcher_Created;
            _fileSystemWatcher.Changed -= FileWatcher_Changed;
            _fileSystemWatcher.Renamed -= FileWatcher_Renamed;
            _fileSystemWatcher.Deleted -= FileWatcher_Deleted;
            _fileSystemWatcher.Dispose();
            _fileSystemWatcher = null;
            DebugHelper.WriteLine("FileSystemWatcher disposed.");
        }
        _fileProvider?.Dispose();
        _fileProvider = null;
    }
}

public class WatchFolderDuplicateEventTimer
{
    private const int ExpireTimeMs = 1000; // Timer expires after 1 second

    private Stopwatch _timer;
    private string? _path;

    /// <summary>
    /// Indicates if the timer has elapsed.
    /// </summary>
    public bool IsElapsed
    {
        get
        {
            return _timer.ElapsedMilliseconds >= ExpireTimeMs;
        }
    }

    /// <summary>
    /// Initializes a new instance of the WatchFolderDuplicateEventTimer.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    public WatchFolderDuplicateEventTimer(string? path)
    {
        _timer = Stopwatch.StartNew();
        _path = path;
    }

    /// <summary>
    /// Checks if the given path is a duplicate event and restarts the timer if it is.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if it's a duplicate event within the expiration time, otherwise false.</returns>
    public bool IsDuplicateEvent(string? path)
    {
        bool result = path == _path && !IsElapsed;
        if (result)
        {
            _timer.Restart(); // Restart the timer for the duplicate event
        }
        return result;
    }
}
