using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
#if WINDOWS
using Windows.Win32.System.SystemInformation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Microsoft.Win32;
#endif

namespace SnapX.Core.Utils;

public static partial class OsInfo
{
    public record WindowsGpuInfo(string Description, string DriverVersion);
    public record WindowsMonitorInfo(string Name, string Position, string Resolution);
    public record WindowsGraphicsInfo(List<WindowsGpuInfo> Gpus, List<WindowsMonitorInfo> Monitors);

    public record LinuxGpuInfo(string Description, string Vendor, string DriverVersion);
    public record LinuxMonitorInfo(string Name, string Resolution, string Coordinates);
    public record LinuxGraphicsInfo(LinuxGpuInfo? Gpu, string? NvidiaDriverVersion, List<LinuxMonitorInfo> Monitors);

    public record MacOSGraphicsInfo(string GpuChipset, string GpuDriverVersion, string MonitorResolution);
    public record GenericGpuInfo(string Description, string DriverVersion, string? Vendor = null);
    public record GenericMonitorInfo(string Name, string Resolution, string? Position = null);
    public record GenericGraphicsInfo(List<GenericGpuInfo>? Gpus, List<GenericMonitorInfo>? Monitors, string OperatingSystemName, string? ErrorMessage = null);

    private static readonly Dictionary<string, string> BuildToFriendlyName = new()
    {
        // Windows 11
        { "22000", "21H2" },
        { "22621", "22H2" },
        { "22631", "23H2" },
        { "26100", "24H2" },
        { "27768", "25H2" },
        { "27813", "25H2" },

        // Windows 10
        { "10240", "1507" },
        { "10586", "1511" },
        { "14393", "1607" },
        { "15063", "1703" },
        { "16299", "1709" },
        { "17134", "1803" },
        { "17763", "1809" },
        { "18362", "1903" },
        { "18363", "1909" },
        { "19041", "2004" },
        { "19042", "20H2" },
        { "19043", "21H1" },
        { "19044", "21H2" },
        { "19045", "22H2" }
    };
    public static string GetFancyOSNameAndVersion()
    {
        if (OperatingSystem.IsWindows()) return GetWindowsVersion();
        if (OperatingSystem.IsLinux()) return GetLinuxVersion();
        if (OperatingSystem.IsMacOS()) return GetmacOSVersion();
        if (OperatingSystem.IsFreeBSD()) return GetFreeBSDVersion();

        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    }
    [SupportedOSPlatform("windows")]
    static string GetWindowsVersion()
    {
        #if WINDOWS
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            if (key == null)
                return $"Windows {Environment.OSVersion.Version}";

            var productName = key.GetValue("ProductName")?.ToString() ?? "Unknown Windows";
            var currentBuild = key.GetValue("CurrentBuild")?.ToString() ?? "Unknown Version";

            if (Helpers.IsWindows11OrGreater())
                productName = productName.Replace("10", "11");
            if (BuildToFriendlyName.TryGetValue(currentBuild, out string friendlyName))
            {
                return $"{productName} {friendlyName}";
            }

            if (int.TryParse(currentBuild, out int currentBuildNumber))
            {
                var closestMatch = BuildToFriendlyName
                    .Where(kvp => int.TryParse(kvp.Key, out int buildNumber))
                    .Select(kvp => new { BuildNumber = int.Parse(kvp.Key), FriendlyName = kvp.Value })
                    .OrderBy(match => Math.Abs(currentBuildNumber - match.BuildNumber))
                    .FirstOrDefault();

                if (closestMatch != null)
                {
                    return $"{productName} {closestMatch.FriendlyName}";
                }
            }

            return $"{productName} {currentBuild}";
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Error getting Windows version, hmm.{Environment.NewLine}{ex.ToString}");
            return $"Windows {Environment.OSVersion.Version}";
        }
        #else
        return "";
        #endif
    }

    static string GetLinuxVersion()
    {
        try
        {
            const string osReleaseFile = "/etc/os-release";
            if (File.Exists(osReleaseFile))
            {
                var lines = File.ReadAllLines(osReleaseFile);

                var prettyName = lines.FirstOrDefault(line => line.StartsWith("PRETTY_NAME"))?.Split('=')[1]?.Trim('"');

                if (!string.IsNullOrEmpty(prettyName)) return prettyName;
                {
                    prettyName = lines.FirstOrDefault(line => line.StartsWith("NAME"))?.Split('=')[1]?.Trim('"');
                    if (string.IsNullOrEmpty(prettyName))
                    {
                        return $"Linux {Environment.OSVersion.Version}";
                    }

                    return prettyName + " " + lines.FirstOrDefault(line => line.StartsWith("VERSION"))?.Split('=')[1]?.Trim('"');
                }
            }

            return $"Linux {Environment.OSVersion.Version}";
        }
        catch
        {
            return $"Linux {Environment.OSVersion.Version}";
        }
    }


    static string GetmacOSVersion()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "sw_vers",
                Arguments = "-productName",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(processStartInfo);
            if (process == null) return RuntimeInformation.OSDescription; // Gracefully fail
            var osName = process.StandardOutput.ReadLine()?.Trim();
            process.WaitForExit();

            processStartInfo.Arguments = "-productVersion";
            process = Process.Start(processStartInfo);
            var version = process?.StandardOutput.ReadLine()?.Trim();
            process?.WaitForExit();

            return $"{osName} {version}";
        }
        catch
        {
            return $"macOS {Environment.OSVersion.Version}";
        }
    }
    static string GetFreeBSDVersion() => $"FreeBSD {Environment.OSVersion.Version}";

    public static string GetProcessorName()
    {
        if (OperatingSystem.IsWindows()) return GetProcessorNameWindows();
        if (OperatingSystem.IsLinux()) return GetProcessorNameLinux();
        if (OperatingSystem.IsMacOS()) return GetProcessorNameMacOS();
        if (OperatingSystem.IsFreeBSD()) return GetProcessorNameFreeBSD();
        return "Unknown Processor";
    }
    [SupportedOSPlatform("windows")]
    private static string GetProcessorNameWindows()
    {
        #if WINDOWS
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key != null)
            {
                var processorName = key.GetValue("ProcessorNameString")?.ToString();
                return processorName ?? "Unknown Processor";
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading registry: " + ex.Message);
        }
        #endif
        return "Unknown Processor";
    }


    private static string GetProcessorNameLinux()
    {
        try
        {
            const string cpuInfoPath = "/proc/cpuinfo";
            var lines = File.ReadAllLines(cpuInfoPath);
            foreach (var line in lines)
            {
                if (!line.StartsWith("model name")) continue;
                var processorName = line[(line.IndexOf(":") + 2)..].Trim();
                return processorName;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading /proc/cpuinfo: " + ex.Message);
        }

        return "Unknown Processor";
    }
    private static string GetProcessorNameFreeBSD()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/sbin/sysctl",
                Arguments = "-n hw.model",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(output))
                    return output;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error running sysctl: " + ex.Message);
        }

        return "Unknown Processor";
    }

    private static string GetProcessorNameMacOS()
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "sysctl";
            process.StartInfo.Arguments = "machdep.cpu.brand_string";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var cpuName = process.StandardOutput.ReadLine()!.Replace("machdep.cpu.brand_string: ", "");
            process.WaitForExit();

            return cpuName?.Trim() ?? "Unknown Processor";
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading sysctl: " + ex.Message);
        }

        return "Unknown Processor";
    }
    public static (long totalMemory, long usedMemory) GetMemoryInfo()
    {
        if (OperatingSystem.IsWindows()) return GetMemoryInfoWindows();
        if (OperatingSystem.IsLinux()) return GetMemoryInfoLinux();
        if (OperatingSystem.IsMacOS()) return GetMemoryInfoMacOS();
        if (OperatingSystem.IsFreeBSD()) return GetMemoryInfoFreeBSD();

        return (0, 0);
    }
    [SupportedOSPlatform("windows5.1.2600")]
    private static (long totalMemory, long usedMemory) GetMemoryInfoWindows()
    {
        #if WINDOWS
        try
        {
            var status = new MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            if (PInvoke.GlobalMemoryStatusEx(ref status))
            {
                var totalMemory = (long)status.ullTotalPhys;
                var freeMemory = GetAvailableMemoryWindows();

                var usedMemory = totalMemory - freeMemory;

                // Return the total and used memory in MiB (1024 * 1024)
                return (totalMemory / (1024 * 1024), usedMemory / (1024 * 1024));
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException("Error reading memory info on Windows: " + ex.Message);
        }
        #endif
        return (0, 0);
    }
    [SupportedOSPlatform("windows5.1.2600")]
    private static long GetAvailableMemoryWindows()
    {
        #if WINDOWS
        var status = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
        };

        if (PInvoke.GlobalMemoryStatusEx(ref status))
        {
            return (long)status.ullAvailPhys;
        }
        DebugHelper.WriteException(new Exception("Unable to retrieve memory information."));
        #endif
        return -1;
    }
    [SupportedOSPlatform("linux")]
    private static (long totalMemory, long usedMemory) GetMemoryInfoLinux()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");

            long totalMemory = 0;
            long availableMemory = 0;
            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal"))
                {
                    totalMemory = ParseMemInfo(line);
                }
                else if (line.StartsWith("MemAvailable"))
                {
                    availableMemory = ParseMemInfo(line);
                }
            }

            long usedMemory = totalMemory - availableMemory;

            return (totalMemory / 1024, usedMemory / 1024);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading memory info on Linux: " + ex.Message);
        }

        return (0, 0);
    }
    [SupportedOSPlatform("freebsd")]
    private static (long totalMemory, long usedMemory) GetMemoryInfoFreeBSD()
    {
        try
        {
            long totalMemory = GetSysctlLong("hw.physmem");
            long freeMemory = GetSysctlLong("vm.stats.vm.v_free_count") * GetPageSize();

            long usedMemory = totalMemory - freeMemory;

            // Convert from bytes to megabytes
            return (totalMemory / 1024 / 1024, usedMemory / 1024 / 1024);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading memory info on FreeBSD: " + ex.Message);
        }

        return (0, 0);
    }
    [SupportedOSPlatform("freebsd")]
    private static long GetSysctlLong(string key)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "/sbin/sysctl",
            Arguments = "-n " + key,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (long.TryParse(output, out var value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Failed to get sysctl value for {key}");
    }

    private static long GetPageSize()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/getconf",
            Arguments = "PAGE_SIZE",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (long.TryParse(output, out var pageSize))
            {
                return pageSize;
            }
        }

        throw new InvalidOperationException("Failed to get page size");
    }

    private static long ParseMemInfo(string line)
    {
        var parts = line.Split(':');
        var value = parts[1].Trim().Split(' ')[0];
        return long.TryParse(value, out long result) ? result : 0;
    }

    private static (long totalMemory, long usedMemory) GetMemoryInfoMacOS()
    {
        try
        {
            var totalMemory = GetSysctl("hw.memsize");
            var freeMemory = GetSysctl("vm.page_free_count");
            var pageSize = GetSysctl("hw.pagesize");

            long freeMemoryBytes = freeMemory * pageSize;
            long usedMemoryBytes = totalMemory - freeMemoryBytes;

            return (totalMemory / (1024 * 1024), usedMemoryBytes / (1024 * 1024));
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading memory info on macOS: " + ex.Message);
        }

        return (0, 0);
    }

    private static long GetSysctl(string key)
    {
        var process = new Process();
        process.StartInfo.FileName = "sysctl";
        process.StartInfo.Arguments = key;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = process.StandardOutput.ReadLine();
        process.WaitForExit();

        if (long.TryParse(output.Split(':')[1].Trim(), out long value))
        {
            return value;
        }

        return 0;
    }
    public static void PrintGraphicsInfo()
    {
        // Now using the generic function for printing
        var genericInfo = GetGenericGraphicsInfo();

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
    /// <summary>
    /// Retrieves graphics information in a generic, platform-agnostic format.
    /// This function is suitable for telemetry as it provides a unified data structure.
    /// </summary>
    /// <returns>A GenericGraphicsInfo object containing GPU and monitor details,
    /// or null if an unhandled error occurs during retrieval.</returns>
     public static GenericGraphicsInfo GetGenericGraphicsInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            var windowsInfo = GetGraphicsInfoWindows();
            if (windowsInfo == null)
                return new GenericGraphicsInfo([], [], "Windows",
                    "Error retrieving Windows graphics info.");
            var genericGpus = windowsInfo.Gpus.Select(g => new GenericGpuInfo(g.Description, g.DriverVersion)).ToList();
            var genericMonitors = windowsInfo.Monitors.Select(m => new GenericMonitorInfo(m.Name, m.Resolution, m.Position)).ToList();
            return new GenericGraphicsInfo(genericGpus, genericMonitors, "Windows");
        }
        else
        {
            if (OperatingSystem.IsLinux())
            {
                var linuxInfo = GetGraphicsInfoLinux();
                if (linuxInfo == null)
                    return new GenericGraphicsInfo([], [], "Linux",
                        "Error retrieving Linux graphics info.");
                var genericGpus = new List<GenericGpuInfo>();
                if (linuxInfo.Gpu != null)
                {
                    genericGpus.Add(new GenericGpuInfo(linuxInfo.Gpu.Description, linuxInfo.Gpu.DriverVersion, linuxInfo.Gpu.Vendor));
                }
                // If a specific NVIDIA driver version from /proc is found, add it as another GPU entry for comprehensive data.
                if (!string.IsNullOrEmpty(linuxInfo.NvidiaDriverVersion))
                {
                    genericGpus.Add(new GenericGpuInfo("NVIDIA Driver (Specific)", linuxInfo.NvidiaDriverVersion, "NVIDIA"));
                }

                var genericMonitorsLinux = linuxInfo.Monitors.Select(m => new GenericMonitorInfo(m.Name, m.Resolution, m.Coordinates)).ToList();

                return new GenericGraphicsInfo(genericGpus, genericMonitorsLinux, "Linux");
            }
            if (!OperatingSystem.IsMacOS())
                return new GenericGraphicsInfo([], [], "Unknown",
                    "This platform is not supported for retrieving generic graphics information.");
            var macosInfo = GetGraphicsInfoMacOS();
            if (macosInfo == null)
                return new GenericGraphicsInfo(new List<GenericGpuInfo>(), new List<GenericMonitorInfo>(), "macOS",
                    "Error retrieving macOS graphics info.");
            var genericGpusmacOS = new List<GenericGpuInfo>
            {
                new(macosInfo.GpuChipset, macosInfo.GpuDriverVersion)
            };
            // macOS `system_profiler` typically provides one resolution value for displays.
            var genericMonitors = new List<GenericMonitorInfo>
            {
                new("Main Display", macosInfo.MonitorResolution)
            };
            return new GenericGraphicsInfo(genericGpusmacOS, genericMonitors, "macOS");
        }
    }
    [SupportedOSPlatform("windows")]
    public static WindowsGraphicsInfo? GetGraphicsInfoWindows()
    {
        try
        {
            const string gpuCommand = """

                                      $gpuInfo = Get-WmiObject Win32_VideoController | Select-Object Description, DriverVersion
                                      foreach ($gpu in $gpuInfo) {
                                          $description = $gpu.Description
                                          $driverVersion = $gpu.DriverVersion

                                          if ($description -like '*nvidia*') {
                                              if ($driverVersion.Length -ge 12) {
                                                  $versionParts = $driverVersion.Split('.')
                                                  if ($versionParts.Length -ge 4) {
                                                      $buildNumberPart = $versionParts[2]
                                                      $revisionNumberPart = $versionParts[3]

                                                      $lastDigitOfBuild = $buildNumberPart.Substring($buildNumberPart.Length - 1, 1)
                                                      $firstTwoDigitsOfRevision = $revisionNumberPart.Substring(0, 2)
                                                      $lastTwoDigitsOfRevision = $revisionNumberPart.Substring($revisionNumberPart.Length - 2, 2)

                                                      $formattedDriverVersion = $lastDigitOfBuild + $firstTwoDigitsOfRevision + '.' + $lastTwoDigitsOfRevision
                                                      Write-Host "GPU: $($description), Driver Version: $($formattedDriverVersion)"
                                                  } else {
                                                      Write-Host "GPU: $($description), Driver Version: $($driverVersion)"
                                                  }
                                              } else {
                                                  Write-Host "GPU: $($description), Driver Version: $($driverVersion)"
                                              }
                                          }
                                          else {
                                              Write-Host "GPU: $($description), Driver Version: $($driverVersion)"
                                          }
                                      }
                                      """;
            var gpuRawOutput = RunPowerShellCommand(gpuCommand);

            const string monitorCommand = """

                                          Add-Type -AssemblyName System.Windows.Forms

                                          [System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
                                              $name = $_.DeviceName
                                              $bounds = $_.Bounds
                                              $position = "X: $($bounds.X), Y: $($bounds.Y)"
                                              $resolution = "$($bounds.Width) x $($bounds.Height)"
                                              Write-Host "Monitor: $name, Position: $position, Resolution: $resolution"
                                          }

                                          """;
            var monitorRawOutput = RunPowerShellCommand(monitorCommand);

            var gpus = ParseWindowsGpuInfo(gpuRawOutput);
            var monitors = ParseWindowsMonitorInfo(monitorRawOutput);

            return new WindowsGraphicsInfo(gpus, monitors);
        }
        catch
        {
            return null;
        }
    }
    private static List<WindowsGpuInfo> ParseWindowsGpuInfo(string rawOutput)
    {
        var lines = rawOutput.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        DebugHelper.WriteLine("GPU Info for this computer:");
        DebugHelper.WriteLine(string.Join("\n", lines));
        var regex = WindowsGPUInfoRegex();

        return (from line in lines select line.Trim() into trimmedLine select regex.Match(trimmedLine) into match where match.Success let name = match.Groups[1].Value.Trim() let driver = match.Groups[2].Value.Trim() select new WindowsGpuInfo(name, driver)).ToList();
    }

    private static List<WindowsMonitorInfo> ParseWindowsMonitorInfo(string rawOutput)
    {
        var lines = rawOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        return (from line in lines where line.StartsWith("Monitor:") select line["Monitor: ".Length..].Split([", Position: ", ", Resolution: "], StringSplitOptions.RemoveEmptyEntries) into parts where parts.Length == 3 let name = parts[0] let positionPart = parts[1] let resolutionPart = parts[2] select new WindowsMonitorInfo(name, positionPart, resolutionPart)).ToList();
    }
    private static string RunPowerShellCommand(string command)
    {
        var escapedCommand = command.Replace("\"", "`\"");
        var process = new Process();
        process.StartInfo.FileName = "powershell";
        process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{escapedCommand}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            DebugHelper.WriteLine($"Error running PowerShell command: {output}");
        }
        return output;
    }
    public static LinuxGraphicsInfo? GetGraphicsInfoLinux()
    {
        try
        {
            LinuxGpuInfo? gpuInfo = null;
            var GpuLspciInfo = RunShellCommand("lspci | grep -i 'vga'");
            if (!string.IsNullOrWhiteSpace(GpuLspciInfo))
            {
                var glxInfo = RunShellCommand("glxinfo | grep -E 'OpenGL version|OpenGL vendor string'");
                var glxLines = glxInfo.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                var driverVersion = glxLines.FirstOrDefault(line => line.Contains("OpenGL version"))?.Split(':')[1]?.Trim();
                var vendor = glxLines.FirstOrDefault(line => line.Contains("OpenGL vendor string"))?.Split(':')[1]?.Trim();
                gpuInfo = new LinuxGpuInfo(GpuLspciInfo, vendor, driverVersion);
            }

            var nvidiaDriverVersion = string.Empty;
            try
            {
                var gpuFileContent = File.ReadAllText("/proc/driver/nvidia/version");
                var firstLine = gpuFileContent.Split(Environment.NewLine)[0];
                var match = NvidiaDriverVersionRegex().Match(firstLine);
                if (match.Success)
                {
                    nvidiaDriverVersion = match.Value;
                }
            }
            catch
            {
                // Swallow errors
            }

            var monitors = new List<LinuxMonitorInfo>();
            try
            {
                var xrandrOutput = RunShellCommand("xrandr --listmonitors");
                var xrandrLines = xrandrOutput.Split('\n');

                for (var i = 1; i < xrandrLines.Length; i++)
                {
                    var line = xrandrLines[i].Trim();
                    if (string.IsNullOrEmpty(line) || !line.Contains('+')) continue;
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var monitorName = parts.Length > 3 ? parts[3] : string.Empty;

                    var resolutionAndCoords = parts.Length > 2 ? parts[2] : string.Empty;

                    // Extract resolution (e.g., "1920x1080") before the first '+' or '/'
                    var resolution = string.Empty;
                    var resolutionMatch = XRandrResolutionRegex().Match(resolutionAndCoords);
                    if (resolutionMatch.Success)
                    {
                        resolution = resolutionMatch.Groups[1].Value;
                    }

                    // Extract coordinates (e.g., "+885+1080")
                    var coordinateParts = resolutionAndCoords.Split('+');
                    var x = coordinateParts.Length > 1 ? coordinateParts[1] : string.Empty;
                    var y = coordinateParts.Length > 2 ? coordinateParts[2] : string.Empty;
                    var coordinates = $"({x}, {y})";

                    monitors.Add(new LinuxMonitorInfo(monitorName, resolution, coordinates));
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine("Error while getting graphics info on Linux");
                DebugHelper.WriteException(ex);
            }

            return new LinuxGraphicsInfo(gpuInfo, nvidiaDriverVersion, monitors);
        }
        catch
        {
            return null;
        }
    }
    public static MacOSGraphicsInfo? GetGraphicsInfoMacOS()
    {
        try
        {
            var output = RunShellCommand("system_profiler SPDisplaysDataType");
            string gpuChipset = string.Empty;
            string gpuDriverVersion = string.Empty;
            string monitorResolution = string.Empty;

            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("Chipset Model"))
                {
                    gpuChipset = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Driver Version"))
                {
                    gpuDriverVersion = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Resolution"))
                {
                    monitorResolution = line.Split(":")[1].Trim();
                }
            }

            return new MacOSGraphicsInfo(gpuChipset, gpuDriverVersion, monitorResolution);
        }
        catch
        {
            return null;
        }
    }

    private static string RunShellCommand(string command)
    {
        var process = new Process();
        process.StartInfo.FileName = "/bin/sh";
        process.StartInfo.Arguments = $"-c \"{command}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
    public static bool IsHdrSupported()
    {
        if (OperatingSystem.IsWindows())
        {
            return CheckWindowsHdr();
        }
        return OperatingSystem.IsMacOS() && CheckMacOSHdr();
        // Detection of HDR on Linux is way too work.
        // If they're on Linux, they should know they're using things like HDR.
    }

    [SupportedOSPlatform("linux")]
    public static bool IsWSL()
    {
        if (!OperatingSystem.IsLinux()) return false;
        try
        {
            return File.ReadAllText("/proc/version").Contains("WSL", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // If we can't read /proc/version, it's likely not running in WSL or there is another error
            return false;
        }
    }
    [SupportedOSPlatform("windows5.0")]
    private static bool CheckWindowsHdr()
    {
        #if WINDOWS
        var hdc = PInvoke.GetDC(new HWND(IntPtr.Zero));
        var bpp = PInvoke.GetDeviceCaps(hdc, GET_DEVICE_CAPS_INDEX.BITSPIXEL);
        return bpp >= 30;
        #else
        return false;
        #endif
    }

    private static bool CheckMacOSHdr()
    {
        string displayInfo = GetMacOSDisplayInfo();
        return displayInfo.Contains("High Dynamic Range");
    }

    private static string GetMacOSDisplayInfo()
    {
        var process = new Process();
        process.StartInfo.FileName = "system_profiler";
        process.StartInfo.Arguments = "SPDisplaysDataType";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    [GeneratedRegex(@"\b(\d+\.\d+\.\d+)\b")]
    private static partial Regex NvidiaDriverVersionRegex();
    [GeneratedRegex(@"^(\d+x\d+)")]
    private static partial Regex XRandrResolutionRegex();
    [GeneratedRegex(@"^GPU:\s*(.*?)(?:,\s*|\s+)Driver Version:\s*(.+)$", RegexOptions.Compiled)]
    private static partial Regex WindowsGPUInfoRegex();
}
