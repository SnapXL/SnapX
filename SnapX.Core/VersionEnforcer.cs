using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapX.Core;


// I might've lost my marbles working on this class.
// Sorry for the bad code, but, it works!

[JsonSerializable(typeof(VersionEnforcer.LockFileContent))]
internal partial class VersionEnforcerContext : JsonSerializerContext;
public class VersionEnforcer : IDisposable
{
    private string _lockFilePath;
    private FileStream? _lockFileStream;
    private string _currentVersion;
    private int _currentPID;
    private bool _startupFailed;
    private bool _ownsLockFile = false;
    internal record LockFileContent
    {
        public int ProcessId { get; set; }
        public string Version { get; set; }
    }
    public VersionEnforcer(string lockDirectory)
    {
        if (string.IsNullOrWhiteSpace(lockDirectory))
        {
            throw new ArgumentException("Lock directory cannot be null or empty.", nameof(lockDirectory));
        }

        try
        {
            Directory.CreateDirectory(lockDirectory);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"VersionEnforcer failed to create lock directory '{lockDirectory}' . Skipping version lock checks. Good luck.");
            DebugHelper.WriteException(ex);
            _startupFailed = true;
        }


        _lockFilePath = Path.Combine(lockDirectory, ".version-lock.json");
        _currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0";
        _currentPID = Environment.ProcessId;
    }

    public void Enforce()
    {
        if (_startupFailed) return;
        for (var i = 0; i < 2; i++)
        {
            var lockFileInfo = ReadLockFileContent();
            var existingVersion = lockFileInfo?.Version;
            try
            {
                if (!string.IsNullOrEmpty(existingVersion))
                {
                    if (existingVersion != _currentVersion)
                    {
                        _lockFileStream.Dispose();
                        throw new InvalidOperationException(
                            $"Found existing instance with version '{existingVersion}', but current version is '{_currentVersion}'. Version mismatch detected.");
                    }

                    var previousInstance = lockFileInfo?.ProcessId;
                    var isPreviousInstanceRunning = lockFileInfo is not null && IsProcessRunning(lockFileInfo.ProcessId);
                    if (lockFileInfo is not null && !isPreviousInstanceRunning)
                    {
                        lockFileInfo = new LockFileContent
                        {
                            ProcessId = _currentPID,
                            Version = _currentVersion
                        };
                        _ownsLockFile = true;
                    }
                    if (lockFileInfo?.ProcessId == _currentPID) _ownsLockFile = true;
                    if (lockFileInfo is not null && _ownsLockFile) WriteVersionInfoToLockFile(lockFileInfo);
                    var statement = !isPreviousInstanceRunning ? $"Took ownership from previous dead instance (same version, PID: {previousInstance})" : $"An existing instance (same version, PID: {lockFileInfo?.ProcessId}) was detected. This is supported. :)";
                    DebugHelper.WriteLine(
                        $"Application (Version: {_currentVersion}) started. {statement}");
                }
                else
                {
                    lockFileInfo ??= new LockFileContent
                    {
                        ProcessId = _currentPID,
                        Version = _currentVersion,
                    };
                    _ownsLockFile = true;
                    WriteVersionInfoToLockFile(lockFileInfo);
                    DebugHelper.WriteLine(
                        $"Application (Version: {_currentVersion}) started. First instance of this version (or lock file was empty).");
                }

                break;
            }
            catch (IOException ex) when (ex.Message.Contains("locked by another process") ||
                                         ex.Message.Contains("being used by another process") ||
                                         ex.HResult == 32) // HResult 32 is common for SharingViolation
            {
                DebugHelper.WriteLine(
                    $"Application (Version: {_currentVersion}) detected another instance running which holds the lock. This instance will try to read its version.");

                try
                {
                    _lockFileStream = new FileStream(_lockFilePath, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite);
                    lockFileInfo = ReadLockFileContent();
                    var lockedVersion = lockFileInfo?.Version;
                    if (string.IsNullOrEmpty(lockedVersion))
                    {
                        throw new InvalidOperationException(
                            $"Lock file '{_lockFilePath}' exists but is empty or unreadable while locked. Cannot verify version.");
                    }

                    if (lockedVersion != _currentVersion)
                    {
                        throw new InvalidOperationException(
                            $"Found existing instance with version '{lockedVersion}', but current version is '{_currentVersion}'. Version mismatch detected.");
                    }

                    if (lockFileInfo is not null && !IsProcessRunning(lockFileInfo.ProcessId))
                    {
                        lockFileInfo = new LockFileContent()
                        {
                            ProcessId = _currentPID,
                            Version = _currentVersion
                        };
                        _ownsLockFile = true;
                        WriteVersionInfoToLockFile(lockFileInfo);
                    }
                    if (lockFileInfo?.ProcessId == _currentPID) _ownsLockFile = true;
                    DebugHelper.WriteLine(
                        $"Application Lock (Version: {_currentVersion}) is compatible with the running instance (same version). Running alongside.");
                    break;
                }
                catch (Exception innerEx)
                {
                    throw new InvalidOperationException(
                        $"Could not read version from existing lock file due to inner error: {innerEx.Message}. Exiting.",
                        innerEx);
                }
            }
            catch (Exception ex)
            {
                _lockFileStream?.Dispose();
                throw new InvalidOperationException($"Fatal error during version enforcement: {ex.Message}.", ex);
            }
        }
    }

    private LockFileContent? ReadLockFileContent()
    {
        _lockFileStream ??= new FileStream(
            _lockFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.Read
        );
        var originalPosition = _lockFileStream.Position;
        try
        {
            _lockFileStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(_lockFileStream, Encoding.UTF8, true, 128, true);
            var json = reader.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(json)) return null;

            return JsonSerializer.Deserialize<LockFileContent>(json, new JsonSerializerOptions()
            {
                TypeInfoResolver = VersionEnforcerContext.Default
            });
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"VersionLock failed to parse/deserialize LockFile at {_lockFilePath}. Nuking file from orbit.");
            DebugHelper.WriteException(ex);
            try
            {
                File.Delete(_lockFilePath);
            } catch {}
            // force downstream caller to redefine stream since it was deleted.
            _lockFileStream = null;
            return null;
        }
        finally
        {
            if (_lockFileStream is not null && _lockFileStream.CanSeek)
            {
                _lockFileStream.Seek(originalPosition, SeekOrigin.Begin);
            }
        }
    }

    private bool IsProcessRunning(int pid)
    {
        if (pid == 0) return false;

        try
        {
            var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch (ArgumentException)
        {
            // Process with pid not found.
            return false;
        }
        catch (InvalidOperationException)
        {
            // Process has exited (may occur if HasExited was not yet updated).
            return false;
        }
        catch (Exception)
        {
            // Other unexpected errors, treat as not running for safety.
            return false;
        }
    }

    private void WriteVersionInfoToLockFile(LockFileContent versionInfo)
    {
        if (_lockFileStream == null)
        {
            DebugHelper.WriteLine($"VersionEnforcer.WriteVersionInfoToLockFile() called when _lockFileStream is null. Redefining.");
            _lockFileStream = new FileStream(
                _lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read
            );
        }

        var json = JsonSerializer.Serialize(versionInfo, new JsonSerializerOptions()
        {
            TypeInfoResolver = VersionEnforcerContext.Default
        });

        _lockFileStream.SetLength(0);
        _lockFileStream.Seek(0, SeekOrigin.Begin);

        using var writer = new StreamWriter(_lockFileStream, Encoding.UTF8, 128, true);
        writer.Write(json);
        writer.Flush();
        // Ensure data is written to disk immediately
        _lockFileStream.Flush(true); // Flush to OS buffers and then to disk
    }

    public void Dispose()
    {
        if (_lockFileStream == null)
        {
            DebugHelper.WriteLine(_ownsLockFile
                ? $"VersionEnforcer owns the lock file at {_lockFilePath} yet _lockFileStream is null! BUG!"
                : $"VersionLock _lockFileStream is null! Lockfile at {_lockFilePath} ");
        }
        if (_ownsLockFile)
        {
            try
            {
                File.Delete(_lockFilePath);
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"VersionLock failed to delete lockfile {_lockFilePath} while disposing.");
                DebugHelper.WriteException(ex);
            }
        }
        else
        {
            DebugHelper.WriteLine($"VersionEnforcer does not own lockfile {_lockFilePath}. Leaving it be.");
        }
        _lockFileStream?.Dispose();
        _lockFileStream = null;
    }
}
