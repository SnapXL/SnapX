using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aptabase.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SnapX.Core.CLI;
using SnapX.Core.Hotkey;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
#if WINDOWS
using SnapX.Core.Utils.Native;
#endif
using SnapX.Core.Watch;
using SQLitePCL;
using Xdg.Directories;


namespace SnapX.Core;
public class SnapX
{
    public const string AppName = "SnapX";
    public static string Qualifier { get; set; } = "";
    public const BuildType Build =

#if DEBUG
            BuildType.Debug;
#elif RPM
            BuildType.RPM;
#elif DEB
            BuildType.DEB;
#elif ARCH
            BuildType.Arch;
#elif APPIMAGE
            BuildType.AppImage;
#elif FLATPAK
            BuildType.Flatpak;
#elif SNAP
            BuildType.Snap;
#elif RUNFILE
            BuildType.Runfile;
#elif HOMEBREW
            BuildType.Homebrew;
#elif PORTABLE
            BuildType.Portable;
#elif BROWSER
            BuildType.Web;
#elif RELEASE
            BuildType.Release;
#else
            BuildType.Unknown;
#endif

    public static string VersionText
    {
        get
        {
            var version = Version.Parse(Helpers.GetApplicationVersion());
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
            if (version.Revision > 0)
                versionString += $".{version.Revision}";
            if (Settings?.DevMode ?? false)
                versionString += " Dev";
            if (Environment.GetEnvironmentVariable("CONTAINER")?.ToLower() == "flatpak")
            {
                versionString += " Flatpak";
            }
            if (Environment.GetEnvironmentVariable("SNAP") != null)
            {
                versionString += " Snap";
            }
            if (Environment.GetEnvironmentVariable("APPIMAGE") != null)
            {
                versionString += " AppImage";
            }
            if (Portable)
                versionString += " Portable";

            return versionString;
        }
    }
    public void setQualifier(string qualifier) => Qualifier = qualifier;
    public static void quit()
    {
        CloseSequence();
    }
    public static string Title
    {
        get
        {
            var title = $"{AppName}{Qualifier}";

            if (Settings.DevMode)
            {
                var info = Build.ToString();

                if (IsAdmin)
                {
                    info += ", Admin";
                }

                title += $" ({info})";
            }

            return title;
        }
    }
    public static bool MultiInstance { get; private set; }
    public static bool Portable { get; private set; }
    public static bool LogToConsole { get; private set; } = true;
    public static bool SilentRun { get; private set; }
    public static bool Sandbox { get; private set; }
    public static bool IsAdmin { get; private set; }
    public static bool SteamFirstTimeConfig { get; private set; }
    public static bool IgnoreHotkeyWarning { get; private set; }
    public static bool PuushMode { get; private set; }

    public static ApplicationConfig? Settings { get; set; }
    public static List<string> Flags { get; set; } = new();

    internal static IConfiguration Configuration { get; set; }
    internal static TaskSettings DefaultTaskSettings { get; set; }
    public static UploadersConfig UploadersConfig { get; set; }
    public static HotkeysConfig HotkeysConfig { get; set; }

    internal static Stopwatch StartTimer { get; private set; }
    internal static HotkeyManager HotkeyManager { get; set; }
    internal static WatchFolderManager WatchFolderManager { get; set; }
    public static SnapXCLIManager CLIManager { get; set; }

    #region Paths

    private const string PersonalPathConfigFileName = "PersonalPath.cfg";

    // Many Windows users consider %USERPROFILE%\Documents\SnapX the correct location,
    // and I'm not here to subvert expectations.
    public static readonly string DefaultPersonalFolder = Path.Combine(OperatingSystem.IsWindows() ? UserDirectory.DocumentsDir : BaseDirectory.DataHome, AppName);
    public static readonly string PortablePersonalFolder = FileHelpers.GetAbsolutePath(AppName);

    private static string PersonalPathConfigFilePath
    {
        get
        {
            string relativePath = FileHelpers.GetAbsolutePath(PersonalPathConfigFileName);

            if (File.Exists(relativePath))
            {
                return relativePath;
            }

            return CurrentPersonalPathConfigFilePath;
        }
    }

    private static readonly string CurrentPersonalPathConfigFilePath = Path.Combine(DefaultPersonalFolder, PersonalPathConfigFileName);

    private static readonly string PreviousPersonalPathConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName, PersonalPathConfigFileName);

    private static readonly string PortableCheckFilePath = FileHelpers.GetAbsolutePath("Portable");
    public static EventAggregator EventAggregator { get; } = new();
    private static string CustomPersonalPath { get; set; }

    private static string CustomConfigPath { get; set; }
    public static SqliteConnection DbConnection { get; set; }
    public static string ShortenPath(string path) =>
        OperatingSystem.IsWindows() ? path : path.Replace(Environment.GetEnvironmentVariable("HOME") ?? "", "~");

    public static string PersonalFolder =>
        !string.IsNullOrEmpty(CustomPersonalPath)
            ? FileHelpers.ExpandFolderVariables(CustomPersonalPath)
            : DefaultPersonalFolder;
    public static string ConfigFolder => string.IsNullOrEmpty(CustomConfigPath)
        ? Path.Combine(
            OperatingSystem.IsWindows()
                ? UserDirectory.DocumentsDir
                : BaseDirectory.ConfigHome,
            AppName)
        : CustomConfigPath;
    public static string CacheFolder => Path.Combine(BaseDirectory.CacheHome, AppName);

    public static string LockDirectory => Path.Combine(BaseDirectory.RuntimeDir, AppName);
    public const string LogsFolderName = "Logs";
    // On Linux, strictly adhere to XDG BaseDirectory spec.
    // On macOS, most of these XDG directories resolve to $HOME/Library/Application Support	anyway so it doesn't really matter.
    public static string LogsFolder => OperatingSystem.IsLinux() ? Path.Combine(BaseDirectory.StateHome, AppName, LogsFolderName) : Path.Combine(PersonalFolder, LogsFolderName);

    public static string LogsFilePath
    {
        get
        {
            if (Settings?.DisableLogging ?? false)
            {
                return string.Empty;
            }
            var date = DateTime.Now;
            return Path.Combine(LogsFolder, date.Year.ToString(), date.Month.ToString("D2"), $"SnapX-{date.Day}.log");
        }
    }


    public static string ScreenshotsParentFolder
    {
        get
        {
            if (Settings is not { UseCustomScreenshotsPath: true }) return Path.Combine(PersonalFolder, "Screenshots");
            var path = Settings.CustomScreenshotsPath;
            var path2 = Settings.CustomScreenshotsPath2;
            if (!string.IsNullOrEmpty(path))
            {
                path = FileHelpers.ExpandFolderVariables(path);

                if (string.IsNullOrEmpty(path2) || Directory.Exists(path))
                {
                    return path;
                }
            }

            if (string.IsNullOrEmpty(path2)) return Path.Combine(PersonalFolder, "Screenshots");
            path2 = FileHelpers.ExpandFolderVariables(path2);

            return Directory.Exists(path2) ? path2 : Path.Combine(PersonalFolder, "Screenshots");
        }
    }

    public static string DBPath => Settings?.SQLitePath ?? Path.Combine(PersonalFolder, "SnapX.db");

    public static string? ImageEffectsFolder => Path.Combine(PersonalFolder, "ImageEffects");

    private static string PersonalPathDetectionMethod;

    public const string HistoryFileNameOld = "History.json";

    public static string? HistoryFilePathOld
    {
        get
        {
            if (Sandbox) return null;

            return Path.Combine(PersonalFolder, HistoryFileNameOld);
        }
    }

    #endregion Paths

    private static bool closeSequenceStarted, restartRequested, restartAsAdmin;

    public void start()
    {
        start([]);
    }

    public void silenceLogging()
    {
        LogToConsole = false;
    }

    public void shutdown()
    {
        CloseSequence();
    }
    private static readonly Timer _optimizeTimer = new(_ =>
    {
        try
        {
            DbConnection?.Execute("PRAGMA optimize;");
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
        }
    }, null, TimeSpan.Zero, TimeSpan.FromDays(1));
    public Assembly[] GetAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
    public void start(string?[] args)
    {
        HandleExceptions();

        StartTimer = Stopwatch.StartNew();
        // TODO: Implement CLI in a better way than what it is now.
        CLIManager = new SnapXCLIManager(args);
        CLIManager.ParseCommands();

        if (CheckAdminTasks()) return; // If SnapX opened just for be able to execute a task as Admin

        UpdatePersonalPath();
        if (CLIManager.IsCommandExist("noconsole")) LogToConsole = false;

        DebugHelper.Init(LogsFilePath);

        MultiInstance = CLIManager.IsCommandExist("multi", "m");
        if (CLIManager.IsCommandExist("sound", "s"))
        {
            DebugHelper.WriteLine("Playing Notification Sound");
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
        }
        Run();
    }

    public long getStartupTime() => StartTimer.ElapsedMilliseconds;
    public EventAggregator getEventAggregator() => EventAggregator;
    public bool isSilent() => SilentRun;
    public static AptabaseClient? aptabaseClient;

    // Supports the failed standard https://consoledonottrack.com/
    public static bool TelemetryEnabled() => !FeatureFlags.DisableTelemetry && !Settings.DisableTelemetry &&
                                    Environment.GetEnvironmentVariable("DO_NOT_TRACK") == null;
    public static bool CanUpload() =>
        !FeatureFlags.DisableUploads && !Settings.DisableUpload;
    public static bool CanAutoUpdate() =>
        !FeatureFlags.DisableAutoUpdates && Settings.AutoCheckUpdate;

    [DapperAot]
    private static void Run()
    {
        DebugHelper.WriteLine("SnapX starting.");
        DebugHelper.WriteLine("Version: " + VersionText);
        DebugHelper.WriteLine("Build: " + Build);
        DebugHelper.WriteLine("Data folder: " + ShortenPath(PersonalFolder));
        DebugHelper.WriteLine("Config folder: " + ShortenPath(ConfigFolder));
        DebugHelper.WriteLine("Cache folder: " + ShortenPath(CacheFolder));

        if (!string.IsNullOrWhiteSpace(PersonalPathDetectionMethod))
        {
            DebugHelper.WriteLine("Personal path detection method: " + PersonalPathDetectionMethod);
        }
        DebugHelper.WriteLine("Operating system: " + SnapXResources.fancyOsName);

        var sessionType = "";
        var desktopEnvironment = "";
        var kdePlasmaMajorVersion = "";

        if (OperatingSystem.IsLinux())
        {
            sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "Unknown";
            desktopEnvironment = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "Unknown";
            kdePlasmaMajorVersion = Environment.GetEnvironmentVariable("KDE_SESSION_VERSION");

            DebugHelper.WriteLine($"Session Type: {sessionType}");
            DebugHelper.WriteLine($"Desktop Environment: {desktopEnvironment}{(desktopEnvironment == "KDE" ? $" {kdePlasmaMajorVersion}" : "")}");
        }
        DebugHelper.WriteLine($"Platform: {Environment.OSVersion.Platform} {Environment.OSVersion.Version}");
        if (OperatingSystem.IsLinux() && OsInfo.IsWSL()) DebugHelper.WriteLine("Running under WSL. Please keep in mind that SnapX defaults to escaping WSL. You can turn this off in settings.");
        DebugHelper.WriteLine(".NET: " + SnapXResources.Dotnet);
        if (Settings is not null) Settings.ApplicationVersion = Helpers.GetApplicationVersion();
        long totalMemory = 0;
        long usedMemory = 0;
        _ = Task.Run(() =>
        {
            DebugHelper.WriteLine($"CPU: {SnapXResources.CPU} ({SnapXResources.CPUCount})");
            (totalMemory, usedMemory) = OsInfo.GetMemoryInfo();
            DebugHelper.WriteLine($"Total Memory: {totalMemory} MiB");
            DebugHelper.WriteLine($"Used Memory: {usedMemory} MiB");
            PrintGraphicsInfo();
            // Linux is not supported for HDR detection.
            if (!OperatingSystem.IsLinux()) DebugHelper.WriteLine($"HDR Capable: {OsInfo.IsHdrSupported()}");
        });
        IsAdmin = Helpers.IsAdministrator();
        DebugHelper.WriteLine("Running as elevated process: " + IsAdmin);

        SilentRun = CLIManager.IsCommandExist("silent", "s");

        IgnoreHotkeyWarning = CLIManager.IsCommandExist("NoHotkeys");

        CreateParentFolders();
        RegisterIntegrations();
        CheckPuushMode();
        DebugWriteFlags();
        if (OperatingSystem.IsFreeBSD() || Environment.GetEnvironmentVariable("SNAPX_USE_SYSTEM_SQLITE3") != null || Environment.GetEnvironmentVariable("SNAPX_PRETEND_FREEBSD") is not null)
        {
            // There are no provided bundles for FreeBSD, must use system SQLite for now.
            // If it fails to load libsqlite3.so, ensure that you have the sqlite package installed. If that doesn't work, try
            // LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib snapx-ui
            raw.SetProvider(new SQLite3Provider_sqlite3());
        }
        else
        {
            raw.SetProvider(new SQLite3Provider_e_sqlite3());
        }

        var dataSource = SnapX.Sandbox ? ":memory:" : DBPath;

        var connectionString = new SqliteConnectionStringBuilder { DataSource = dataSource, Mode = SqliteOpenMode.ReadWriteCreate, ForeignKeys = true }.ToString();
        DbConnection = new SqliteConnection(connectionString);
        RunWithTimeout(() => DbConnection.OpenAsync(), $"Opening the database connection at {DBPath}");
        RunWithTimeout(() => DbConnection.ExecuteAsync("PRAGMA journal_mode=WAL;"), "Setting journal mode");
        _ = Task.Run(() =>
        {
            try
            {
                if (DbConnection != null)
                    RunWithTimeout(() => DbConnection.ExecuteAsync("PRAGMA optimize=0x10002;"), "Optimizing database");
            }
            catch (ObjectDisposedException) { } // shush
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex);
            }
        });
        // Handle SnapX running for a long time. To be good SQLite user, you must tidy up database once in a while.
        DebugHelper.WriteLine($"DB: SQLite {DbConnection.ServerVersion}");
        DebugHelper.WriteLine($"DB Path: {ShortenPath(DbConnection.ConnectionString)}");

        DbConnection.StateChange += (_, Args) =>
        {
            DebugHelper.WriteLine($"DB: {Args.CurrentState}");
        };
        SettingManager.LoadInitialSettings();
        if (TelemetryEnabled())
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });
            var logger = loggerFactory.CreateLogger<AptabaseClient>();

            var aptabaseClient = new AptabaseClient("A-US-4105716286", new AptabaseOptions
            {
                EnablePersistence = true,
#if DEBUG
                IsDebugMode = true
#else
                IsDebugMode = false
#endif
            }, logger);
            var telemetry = new Telemetry(DbConnection, aptabaseClient);
            SnapX.aptabaseClient = aptabaseClient;
            telemetry.TrackEvent("app_started", new Dictionary<string, object>
            {
                { "CPU", SnapXResources.CPU },
                { "CPUCount", SnapXResources.CPUCount},
                { "Build", $"{Build}" },
                {
                    "GPUS",
                    SnapXResources.graphicsInfo.Gpus is { Count: > 0 } gpus
                        ? string.Join(", ", gpus.Select(gpu => $"{gpu.Description} ({gpu.DriverVersion})"))
                        : string.Empty
                },
                { "Monitors", string.Join(", ", SnapXResources.graphicsInfo?.Monitors ?? []) },
                { "totalMemory", totalMemory },
                { "usedMemory", usedMemory },
                { "Dotnet", SnapXResources.Dotnet },
                { "fancyOsName", SnapXResources.fancyOsName},
                { "DbVersion", DbConnection.ServerVersion },
                { "DesktopEnvironment", $"{desktopEnvironment}{(desktopEnvironment == "KDE" ? $" {kdePlasmaMajorVersion}" : "")}"},
                { "SessionType", sessionType },
                { "Flags", string.Join(",", Flags)}
            });
            SentrySdk.Init(options =>
            {
                // This allows end users to test themselves what data is sent to Sentry
                var sentryDsnEnv = Environment.GetEnvironmentVariable("SENTRY_DSN");
                options.Dsn = !string.IsNullOrWhiteSpace(sentryDsnEnv) ? sentryDsnEnv : "https://e0a07df30c8b96560f93b10cf4338eba@o4504136997928960.ingest.us.sentry.io/4508785180737536";

                // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
                options.Debug = Environment.GetEnvironmentVariable("SENTRY_DEBUG") == "1";

#if DEBUG
                options.Environment = "development";
                options.CreateHttpMessageHandler = () => new LoggingHttpMessageHandler(new SentryHttpMessageHandler(HttpClientFactory.Handler), DebugHelper.Logger);
                options.DisableSentryHttpMessageHandler = true;
#else
                options.Environment = "production";
                options.CreateHttpMessageHandler = () => HttpClientFactory.Handler;
#endif
                options.ConfigureClient = client =>
                {
                    var snapXHttpClient = HttpClientFactory.Get();

                    foreach (var header in snapXHttpClient.DefaultRequestHeaders)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    client.DefaultRequestVersion = snapXHttpClient.DefaultRequestVersion;
                    client.DefaultVersionPolicy = snapXHttpClient.DefaultVersionPolicy;
                };
                // VLCException includes multiple paths with username
                // For full transparency, I discovered this issue on my computer.
                // No other users are effected to my knowledge.
                options.SetBeforeSend((sentryEvent, _) =>
                {
                    if (sentryEvent.Exception != null
                        && !string.IsNullOrEmpty(sentryEvent.Exception.Message))
                    {
                        if (sentryEvent.Exception.Message.Contains(Environment.UserName)) return null;
                    }
                    Task.Run(() => LogTelemetry(sentryEvent, telemetry));
                    return sentryEvent;
                });

                // Enabling this option is recommended for client applications only. It ensures all threads use the same global scope.
                options.IsGlobalModeEnabled = true;
                // This option is recommended. It enables Sentry's "Release Health" feature.
                options.AutoSessionTracking = true;

                // Set TracesSampleRate to 1.0 to capture 100%
                // of transactions for tracing.
                options.TracesSampleRate = 0.2;

                // Sample rate for profiling, applied on top of the TracesSampleRate,
                // e.g. 0.2 means we want to profile 20 % of the captured transactions.
                // We recommend adjusting this value in production.
                options.ProfilesSampleRate = 0.2;
                options.AddIntegration(new ProfilingIntegration());

                // This saves events for later when internet connectivity is poor/not working.
                options.CacheDirectoryPath = Path.Combine(BaseDirectory.CacheHome, AppName);
            });
        }
        if (CLIManager.IsCommandExist("noconsole")) LogToConsole = false;
        // CleanupManager.CleanupAsync();
    }
    static void RunWithTimeout(Func<Task> taskFactory, string description = "SQLite Code", int timeoutSeconds = 10)
    {
        var task = Task.Run(taskFactory); // force SQLite to run on a background thread, HAH!

        var completedInTime = Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)))
            .GetAwaiter()
            .GetResult() == task;

        if (!completedInTime)
            throw new TimeoutException($"{description} timed out after {timeoutSeconds} seconds.");

        task.GetAwaiter().GetResult(); // propagate exceptions
    }
    public static void PrintGraphicsInfo()
    {
        var genericInfo = SnapXResources.graphicsInfo;

        if (genericInfo != null)
        {
            DebugHelper.WriteLine($"Graphics Information for {genericInfo.OperatingSystemName}:");
            if (genericInfo.Gpus != null && genericInfo.Gpus.Count != 0)
            {
                foreach (var gpu in genericInfo.Gpus)
                {
                    DebugHelper.WriteLine($"GPU: {gpu.Description}, Driver Version: {gpu.DriverVersion}{(gpu.Vendor != null ? $", Vendor: {gpu.Vendor}" : "")}");
                }
            }
            else
            {
                DebugHelper.WriteLine("No GPU information found.");
            }

            if (genericInfo.Monitors != null && genericInfo.Monitors.Any())
            {
                foreach (var monitor in genericInfo.Monitors)
                {
                    DebugHelper.WriteLine($"Monitor: {monitor.Name}, Resolution: {monitor.Resolution}{(monitor.Position != null ? $", Position: {monitor.Position}" : "")}");
                }
            }
            else
            {
                DebugHelper.WriteLine("No Monitor information found.");
            }

            if (!string.IsNullOrEmpty(genericInfo.ErrorMessage))
            {
                DebugHelper.WriteLine($"Error: {genericInfo.ErrorMessage}");
            }
        }
        else
        {
            DebugHelper.WriteLine("Failed to retrieve generic graphics information.");
        }
    }
    public SnapXCLIManager GetCLIManager() => CLIManager;
    public ApplicationConfig GetConfiguration() => Settings;
    public UploadersConfig GetUploadersConfig() => UploadersConfig;
    public HotkeysConfig GetHotkeysConfig() => HotkeysConfig;


    public static void CloseSequence()
    {
        if (closeSequenceStarted) return;
        closeSequenceStarted = true;

        DebugHelper.WriteLine("SnapX closing!");
        TaskManager.StopAllTasks();
        WatchFolderManager?.Dispose();
        SettingManager.SaveAllSettings();
        SettingManager.Dispose();
        if (DbConnection != null)
        {
            DbConnection.Close();
            DbConnection.Dispose();
        }

        if (TelemetryEnabled())
        {
            aptabaseClient?.DisposeAsync().GetAwaiter().GetResult();
        }

        DebugHelper.WriteLine("SnapX closed.");
        DebugHelper.FlushBufferedMessages();
        Environment.Exit(0);
    }

    private static void UpdatePersonalPath()
    {
        if (Sandbox) return;
        Sandbox = CLIManager.IsCommandExist("sandbox") || Environment.GetEnvironmentVariable("SNAPX_SANDBOX") != null;

        if (CLIManager.IsCommandExist("portable", "p"))
        {
            Portable = true;
            CustomPersonalPath = PortablePersonalFolder;
            PersonalPathDetectionMethod = "Portable CLI flag";
        }
        if (File.Exists(PortableCheckFilePath))
        {
            Portable = true;
            CustomPersonalPath = PortablePersonalFolder;
            PersonalPathDetectionMethod = $"Portable file ({PortableCheckFilePath})";
        }
        else
        {
            MigratePersonalPathConfig();

            string? customPersonalPath = ReadPersonalPathConfig();

            if (!string.IsNullOrEmpty(customPersonalPath))
            {
                CustomPersonalPath = FileHelpers.GetAbsolutePath(customPersonalPath);
                PersonalPathDetectionMethod = $"PersonalPath.cfg file ({PersonalPathConfigFilePath})";
            }
        }

        if (!Directory.Exists(PersonalFolder))
        {
            try
            {
                Directory.CreateDirectory(PersonalFolder);
            }
            catch (Exception e)
            {
                var sb = new StringBuilder();

                sb.AppendFormat("{0} \"{1}\"", "Unable to create personal folder!", PersonalFolder);
                sb.AppendLine();

                if (!string.IsNullOrEmpty(PersonalPathDetectionMethod))
                {
                    sb.AppendLine("Personal path detection method: " + PersonalPathDetectionMethod);
                }

                sb.AppendLine();
                sb.Append(e);

                CustomPersonalPath = "";
            }
        }
        if (!Directory.Exists(ConfigFolder)) FileHelpers.CreateDirectory(ConfigFolder);
    }

    private static void CreateParentFolders()
    {
        if (Sandbox || !Directory.Exists(PersonalFolder)) return;
        FileHelpers.CreateDirectory(LockDirectory);
        FileHelpers.CreateDirectory(SettingManager.SnapshotFolder);
        FileHelpers.CreateDirectory(ImageEffectsFolder);
        FileHelpers.CreateDirectory(ScreenshotsParentFolder);
    }

    private static void RegisterIntegrations()
    {
        if (Portable || Sandbox) return;

#if WINDOWS
        WindowsAPI.RegisterWindowsIntegrations();
#endif
    }

    private static void MigratePersonalPathConfig()
    {
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                // @see https://github.com/SnapXL/SnapX/blob/c650e315ab51e9100e4c63d61e5915fcf530d96c/Progress.md
                var InformalPath = Path.Join(UserDirectory.DocumentsDir, AppName);
                if (Directory.Exists(UserDirectory.DocumentsDir) && !Directory.Exists(InformalPath) && !File.Exists(InformalPath)) Directory.CreateSymbolicLink(InformalPath, PersonalFolder);
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException)
                {
                    return;
                }
                if (e.HResult == -2147024816 ||
                    e.HResult == -2147024713)
                {
                    // The file already exists, ignore.
                    return;
                }
                DebugHelper.WriteLine("Failed to symbolic link typical SnapX path. You can safely ignore this.");
                DebugHelper.WriteException(e);
            }
        }

        if (!File.Exists(PreviousPersonalPathConfigFilePath)) return;
        try
        {
            if (!File.Exists(CurrentPersonalPathConfigFilePath))
            {
                FileHelpers.CreateDirectoryFromFilePath(CurrentPersonalPathConfigFilePath);
                FileHelpers.CreateDirectoryFromFilePath(ConfigFolder);
                File.Move(PreviousPersonalPathConfigFilePath, CurrentPersonalPathConfigFilePath);
            }
            File.Delete(PreviousPersonalPathConfigFilePath);
            Directory.Delete(Path.GetDirectoryName(PreviousPersonalPathConfigFilePath)!);
        }
        catch (Exception e)
        {
            e.ShowError();
        }
    }

    public static string ReadPersonalPathConfig()
    {
        return File.Exists(PersonalPathConfigFilePath)
            ? File.ReadAllText(PersonalPathConfigFilePath, Encoding.UTF8).Trim()
            : string.Empty;
    }

    public static bool WritePersonalPathConfig(string path)
    {
        path = path.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(path) && !File.Exists(PersonalPathConfigFilePath))
            return false;

        var currentPath = ReadPersonalPathConfig();

        if (path.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            FileHelpers.CreateDirectoryFromFilePath(PersonalPathConfigFilePath);
            File.WriteAllText(PersonalPathConfigFilePath, path, Encoding.UTF8);
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            DebugHelper.WriteException(e);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            e.ShowError();
        }

        return false;
    }

    private static void HandleExceptions()
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            return;
        }
#endif

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += ((_, _) => CloseSequence());
    }
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) => OnError((Exception)e.ExceptionObject);
    private static void OnError(Exception e) => DebugHelper.WriteException(e);

    private static bool CheckAdminTasks()
    {
        if (CLIManager.IsCommandExist("dnschanger"))
        {
            return true;
        }

        return false;
    }

    private static bool CheckPuushMode()
    {
        var puushPath = FileHelpers.GetAbsolutePath("puush");
        PuushMode = File.Exists(puushPath);
        return PuushMode;
    }

    private static void DebugWriteFlags()
    {
        void AddFlagIfTrue(bool condition, string flagName)
        {
            if (condition)
            {
                Flags.Add(flagName);
            }
        }

        AddFlagIfTrue(Settings?.DevMode ?? false, nameof(Settings.DevMode));
        AddFlagIfTrue(MultiInstance, nameof(MultiInstance));
        AddFlagIfTrue(Portable, nameof(Portable));
        AddFlagIfTrue(SilentRun, nameof(SilentRun));
        AddFlagIfTrue(Sandbox, nameof(Sandbox));
        AddFlagIfTrue(IgnoreHotkeyWarning, nameof(IgnoreHotkeyWarning));
        AddFlagIfTrue(FeatureFlags.DisableTelemetry, nameof(FeatureFlags.DisableTelemetry));
        AddFlagIfTrue(FeatureFlags.DisableAutoUpdates, nameof(FeatureFlags.DisableAutoUpdates));
        AddFlagIfTrue(FeatureFlags.DisableUploads, nameof(FeatureFlags.DisableUploads));
        AddFlagIfTrue(FeatureFlags.DisableOCR, nameof(FeatureFlags.DisableOCR));
        AddFlagIfTrue(PuushMode, nameof(PuushMode));

        var output = string.Join(", ", Flags);
        DebugHelper.WriteLine("Flags: " + output);
    }

    private static void LogTelemetry(SentryEvent sentryEvent, Telemetry telemetry)
    {
        var json = new JsonObject
        {
            ["event_id"] = sentryEvent.EventId.ToString(),
            ["timestamp"] = sentryEvent.Timestamp.ToString("o") // ISO 8601 format
        };

        if (sentryEvent.Message != null)
        {
            json["logentry"] = new JsonObject
            {
                ["message"] = sentryEvent.Message.Formatted
            };
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.Logger))
        {
            json["logger"] = sentryEvent.Logger;
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.Platform))
        {
            json["platform"] = sentryEvent.Platform;
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.ServerName))
        {
            json["server_name"] = sentryEvent.ServerName;
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.Release))
        {
            json["release"] = sentryEvent.Release;
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.Distribution))
        {
            json["dist"] = sentryEvent.Distribution;
        }

        if (sentryEvent.SentryExceptions != null)
        {
            var exceptionArray = new JsonArray();
            foreach (var sentryException in sentryEvent.SentryExceptions)
            {
                var exceptionJson = new JsonObject
                {
                    ["type"] = sentryException.Type,
                    ["value"] = sentryException.Value
                };
                exceptionArray.Add(exceptionJson);
            }
            json["exception"] = new JsonObject
            {
                ["values"] = exceptionArray
            };
        }

        if (sentryEvent.SentryThreads != null)
        {
            var threadArray = new JsonArray();
            foreach (var sentryThread in sentryEvent.SentryThreads)
            {
                var threadJson = new JsonObject
                {
                    ["id"] = sentryThread.Id,
                    ["name"] = sentryThread.Name
                };
                threadArray.Add(threadJson);
            }
            json["threads"] = new JsonObject
            {
                ["values"] = threadArray
            };
        }

        if (sentryEvent.Level.HasValue)
        {
            json["level"] = sentryEvent.Level.ToString().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.TransactionName))
        {
            json["transaction"] = sentryEvent.TransactionName;
        }

        if (sentryEvent.Request != null)
        {
            var requestJson = new JsonObject
            {
                ["url"] = sentryEvent.Request.Url,
                ["method"] = sentryEvent.Request.Method
            };
            json["request"] = requestJson;
        }

        if (sentryEvent.Contexts != null)
        {
            var contextsJson = new JsonObject();
            foreach (var context in sentryEvent.Contexts)
            {
                // SentryContexts continues to get harder to serialize, so instead enjoy the STRING of what the object is called!
                contextsJson[context.Key] = context.Value.ToString();
            }
            json["contexts"] = contextsJson;
        }

        if (sentryEvent.User != null)
        {
            var userJson = new JsonObject
            {
                ["id"] = sentryEvent.User.Id,
                ["email"] = sentryEvent.User.Email,
                ["username"] = sentryEvent.User.Username
            };
            json["user"] = userJson;
        }

        if (!string.IsNullOrWhiteSpace(sentryEvent.Environment))
        {
            json["environment"] = sentryEvent.Environment;
        }

        if (sentryEvent.Sdk != null)
        {
            var sdkJson = new JsonObject
            {
                ["name"] = sentryEvent.Sdk.Name,
                ["version"] = sentryEvent.Sdk.Version
            };
            json["sdk"] = sdkJson;
        }

        if (sentryEvent.Breadcrumbs.Any())
        {
            var breadcrumbsJson = new JsonArray();
            foreach (var breadcrumb in sentryEvent.Breadcrumbs)
            {
                var breadcrumbJson = new JsonObject
                {
                    ["message"] = breadcrumb.Message,
                    ["timestamp"] = breadcrumb.Timestamp.ToString("o")
                };
                breadcrumbsJson.Add(breadcrumbJson);
            }
            json["breadcrumbs"] = breadcrumbsJson;
        }

        if (sentryEvent.Extra.Any())
        {
            var extraJson = new JsonObject();
            foreach (var kvp in sentryEvent.Extra)
            {
                extraJson[kvp.Key] = JsonSerializer.SerializeToNode(kvp.Value);
            }
            json["extra"] = extraJson;
        }

        if (sentryEvent.Tags.Any())
        {
            var tagsJson = new JsonObject();
            foreach (var tag in sentryEvent.Tags)
            {
                tagsJson[tag.Key] = tag.Value;
            }
            json["tags"] = tagsJson;
        }

        telemetry.LogTelemetry("Sentry", sentryEvent.TransactionName ?? sentryEvent.Exception?.Message ?? "SentryEvent", json.ToJsonString());
    }
}

