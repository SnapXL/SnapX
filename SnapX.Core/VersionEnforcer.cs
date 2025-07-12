using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Interfaces;

namespace SnapX.Core;


[JsonSerializable(typeof(VersionEnforcer.LockFileContent))]
internal sealed partial class VersionEnforcerContext : JsonSerializerContext;
public sealed class VersionEnforcer : IDisposable
{
    private ILoggerService _logger;
    private readonly string _lockFilePath;
    private FileStream? _lockFileStream;
    private readonly string _currentVersion;
    private readonly int _currentPID;
    private readonly bool _startupFailed;
    private bool _ownsLockFile;

    internal record LockFileContent
    {
        public required int ProcessId { get; set; }
        public required string Version { get; set; }
    }
    public VersionEnforcer(string lockDirectory, ILoggerService logger)
    {
        if (string.IsNullOrWhiteSpace(lockDirectory))
        {
            throw new ArgumentException($"Lock directory cannot be null or empty {nameof(lockDirectory)}");
        }

        _logger = logger;

        try
        {
            Directory.CreateDirectory(lockDirectory);
        }
        catch (Exception ex)
        {
            _logger.Information("VersionEnforcer failed to create lock directory \'{LockDirectory}\' . Skipping version lock checks. Good luck", lockDirectory);
            _logger.Error(ex.ToString());
            _startupFailed = true;
        }


        _lockFilePath = Path.Combine(lockDirectory, ".version-lock.json");
        _currentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0";
        _currentPID = Environment.ProcessId;
    }

    public void Enforce()
    {
        if (_startupFailed) return;
        ThreadPool.QueueUserWorkItem(_ => AttemptEnforcementWithRetries());
    }

    private void AttemptEnforcementWithRetries()
    {
        for (var i = 0; i < 2; i++)
        {
            if (TryEnforceInternal())
            {
                break;
            }
        }
    }

    private bool TryEnforceInternal()
    {
        try
        {
            EnforceVersionAndOwnership();
            return true;
        }
        catch (IOException ex) when (IsLockFileInUseException(ex))
        {
            HandleLockFileInUse();
            return false;
        }
        catch (Exception ex)
        {
            _lockFileStream?.Dispose();
            throw new InvalidOperationException($"Fatal error during version enforcement: {ex.Message}.", ex);
        }
    }

    private bool IsLockFileInUseException(IOException ex)
    {
        return ex.Message.Contains("locked by another process") ||
               ex.Message.Contains("being used by another process") ||
               ex.HResult == 32;
    }

    private void EnforceVersionAndOwnership()
    {
        var lockFileInfo = ReadLockFileContent();
        var existingVersion = lockFileInfo?.Version;

        if (!string.IsNullOrEmpty(existingVersion))
        {
            HandleExistingVersion(lockFileInfo, existingVersion);
        }
        else
        {
            HandleNoExistingVersion(ref lockFileInfo);
        }
    }

    private void HandleExistingVersion(LockFileContent? lockFileInfo, string existingVersion)
    {
        if (existingVersion != _currentVersion)
        {
            _lockFileStream?.Dispose();
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

        var statement = !isPreviousInstanceRunning
            ? $"Took ownership from previous dead instance (same version, PID: {previousInstance})"
            : $"An existing instance (same version, PID: {lockFileInfo?.ProcessId}) was detected. This is supported. :)";
        _logger.Information("Application (Version: {CurrentVersion}) started. {Statement}", _currentVersion, statement);
    }

    private void HandleNoExistingVersion(ref LockFileContent? lockFileInfo)
    {
        lockFileInfo ??= new LockFileContent
        {
            ProcessId = _currentPID,
            Version = _currentVersion,
        };
        _ownsLockFile = true;
        WriteVersionInfoToLockFile(lockFileInfo);
        _logger.Information("Application (Version: {CurrentVersion}) started. First instance of this version (or lock file was empty)", _currentVersion);
    }

    private void HandleLockFileInUse()
    {
        _logger.Information("Application (Version: {CurrentVersion}) detected another instance running which holds the lock. This instance will try to read its version", _currentVersion);

        try
        {
            _lockFileStream = new FileStream(_lockFilePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite);
            var lockFileInfo = ReadLockFileContent();
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
            _logger.Information("Application Lock (Version: {CurrentVersion}) is compatible with the running instance (same version). Running alongside", _currentVersion);
        }
        catch (Exception innerEx)
        {
            throw new InvalidOperationException(
                $"Could not read version from existing lock file due to inner error: {innerEx.Message}. Exiting.",
                innerEx);
        }
    }

    private LockFileContent? ReadLockFileContent()
    {
        _lockFileStream ??= new FileStream(
            _lockFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite
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
            _logger.Information("VersionLock failed to parse/deserialize LockFile at {LockFilePath}. Nuking file from orbit", _lockFilePath);
            DebugHelper.WriteException(ex);
            try
            {
                File.Delete(_lockFilePath);
            }
            catch
            {
                // Suppress any errors.
            }
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

    private static bool IsProcessRunning(int pid)
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
            // The process has exited (may occur if HasExited was not yet updated).
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
            _logger.Information($"VersionEnforcer.WriteVersionInfoToLockFile() called when _lockFileStream is null. Redefining.");
            _lockFileStream = new FileStream(
                _lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite
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
            _logger.Information(_ownsLockFile
                ? string.Format($"VersionEnforcer owns the lock file at {{0}} yet {nameof(_lockFileStream)} is null! BUG!",
                    _lockFilePath)
                : string.Format($"VersionLock {nameof(_lockFileStream)} is null! Lockfile at {{0}} ", _lockFilePath));
        }
        var lockfileInfo = ReadLockFileContent();
        if (lockfileInfo is not null && !IsProcessRunning(lockfileInfo.ProcessId)) _ownsLockFile = true;
        if (_ownsLockFile)
        {
            try
            {
                _lockFileStream?.Close();
                File.Delete(_lockFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "VersionLock failed to delete lockfile {LockFilePath} while disposing", _lockFilePath);
            }
        }
        else
        {
            _logger.Information("VersionEnforcer does not own lockfile {LockFilePath}. Leaving it be", _lockFilePath);
        }
        _lockFileStream?.Dispose();
        _lockFileStream = null;
    }
}
