using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SnapX.Core.Utils;

public static partial class OsInfo
{
    public static string GetFancyOSNameAndVersion()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetWindowsVersion();

        }
        else if (OperatingSystem.IsLinux())
        {
            return GetLinuxVersion();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return GetmacOSVersion();
        }
        else
        {
            return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
        }
    }
    [SupportedOSPlatform("windows")]
    static string GetWindowsVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            if (key == null)
                return $"Windows {Environment.OSVersion.Version}";

            var productName = key.GetValue("ProductName")?.ToString() ?? "Unknown Windows";
            var currentBuild = key.GetValue("CurrentBuild")?.ToString() ?? "Unknown Version";

            if (Helpers.IsWindows11OrGreater())
                productName = productName.Replace("10", "11");

            return $"{productName} {currentBuild}";

        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Error getting Windows version, hmm.{Environment.NewLine}{ex.ToString}");
            return $"Windows {Environment.OSVersion.Version}";
        }
    }

    static string GetLinuxVersion()
    {
        try
        {
            var osReleaseFile = "/etc/os-release";
            if (File.Exists(osReleaseFile))
            {
                var lines = File.ReadAllLines(osReleaseFile);

                var prettyName = lines.FirstOrDefault(line => line.StartsWith("PRETTY_NAME"))?.Split('=')[1]?.Trim('"');

                if (string.IsNullOrEmpty(prettyName))
                {
                    prettyName = lines.FirstOrDefault(line => line.StartsWith("NAME"))?.Split('=')[1]?.Trim('"');
                    if (string.IsNullOrEmpty(prettyName))
                    {
                        return $"Linux {Environment.OSVersion.Version}";
                    }

                    return prettyName + " " + lines.FirstOrDefault(line => line.StartsWith("VERSION"))?.Split('=')[1]?.Trim('"');
                }

                return prettyName;
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
            var osName = process.StandardOutput.ReadLine().Trim();
            process.WaitForExit();

            processStartInfo.Arguments = "-productVersion";
            process = Process.Start(processStartInfo);
            var version = process.StandardOutput.ReadLine().Trim();
            process.WaitForExit();

            return $"{osName} {version}";
        }
        catch
        {
            return $"macOS {Environment.OSVersion.Version}";
        }
    }

    public static string GetProcessorName()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetProcessorNameWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            return GetProcessorNameLinux();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return GetProcessorNameMacOS();
        }
        else
        {
            throw new PlatformNotSupportedException("This platform is not supported.");
        }
    }
    [SupportedOSPlatform("windows")]
    private static string GetProcessorNameWindows()
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
            {
                if (key != null)
                {
                    var processorName = key.GetValue("ProcessorNameString")?.ToString();
                    return processorName ?? "Unknown Processor";
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading registry: " + ex.Message);
        }

        return "Unknown Processor";
    }


    private static string GetProcessorNameLinux()
    {
        try
        {
            var cpuInfoPath = "/proc/cpuinfo";
            var lines = File.ReadAllLines(cpuInfoPath);
            foreach (var line in lines)
            {
                if (line.StartsWith("model name"))
                {
                    var processorName = line.Substring(line.IndexOf(":") + 2).Trim();
                    return processorName;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading /proc/cpuinfo: " + ex.Message);
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
        if (OperatingSystem.IsWindows())
        {
            return GetMemoryInfoWindows();
        }
        if (OperatingSystem.IsLinux())
        {
            return GetMemoryInfoLinux();
        }
        if (OperatingSystem.IsMacOS())
        {
            return GetMemoryInfoMacOS();
        }
        throw new PlatformNotSupportedException("This platform is not supported.");
    }
    [SupportedOSPlatform("windows")]
    private static (long totalMemory, long usedMemory) GetMemoryInfoWindows()
    {
        try
        {
            var status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

            if (GlobalMemoryStatusEx(ref status))
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

        return (0, 0);
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
    [SupportedOSPlatform("windows")]
    private static long GetAvailableMemoryWindows()
    {
        MEMORYSTATUSEX status = new MEMORYSTATUSEX();
        status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

        if (GlobalMemoryStatusEx(ref status))
        {
            return (long)status.ullAvailPhys;
        }
        DebugHelper.WriteException(new Exception("Unable to retrieve memory information."));
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
        var process = new System.Diagnostics.Process();
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
        if (OperatingSystem.IsWindows())
        {
            PrintGraphicsInfoWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            PrintGraphicsInfoLinux();
        }
        else if (OperatingSystem.IsMacOS())
        {
            PrintGraphicsInfoMacOS();
        }
        else
        {
            DebugHelper.WriteLine("This platform is not supported for printing graphics information.");
        }
    }
    [SupportedOSPlatform("windows")]
    private static void PrintGraphicsInfoWindows()
    {
        try
        {
            // PowerShell script to get GPU info and format driver version for NVIDIA GPUs
            var command = @"
$gpuInfo = Get-WmiObject Win32_VideoController | Select-Object Description, DriverVersion
foreach ($gpu in $gpuInfo) {
    $description = $gpu.Description
    $driverVersion = $gpu.DriverVersion

    if ($description -like '*nvidia*') {
        # Check if it's an NVIDIA GPU
        if ($driverVersion.Length -ge 12) {
            # Check if driver version length is at least 12 characters (expected format length)
            $versionParts = $driverVersion.Split('.')
            if ($versionParts.Length -ge 4) {
                # Check if driver version has at least 4 parts separated by dots
                $buildNumberPart = $versionParts[2] # The 3rd part is assumed to be build number (e.g., ""15"")
                $revisionNumberPart = $versionParts[3] # The 4th part is assumed to be revision number (e.g., ""7216"")

                $lastDigitOfBuild = $buildNumberPart.Substring($buildNumberPart.Length - 1, 1) # Extract last digit of build number
                $firstTwoDigitsOfRevision = $revisionNumberPart.Substring(0, 2)          # Extract first two digits of revision number
                $lastTwoDigitsOfRevision = $revisionNumberPart.Substring($revisionNumberPart.Length - 2, 2) # Extract last two digits of revision number

                # Format the driver version as: (last digit of build).(first two digits of revision)(last two digits of revision)
                $formattedDriverVersion = $lastDigitOfBuild + $firstTwoDigitsOfRevision + '.' + $lastTwoDigitsOfRevision
                Write-Host ""GPU: $($description), Driver Version: $($formattedDriverVersion)""
            } else {
                # If less than 4 parts, output raw driver version
                Write-Host ""GPU: $($description), Driver Version: $($driverVersion)""
            }
        } else {
            # If driver version length is less than 12 characters, output raw driver version
            Write-Host ""GPU: $($description), Driver Version: $($driverVersion)""
        }
    }
    else {
        # For non-NVIDIA GPUs, output raw driver version
        Write-Host ""GPU: $($description), Driver Version: $($driverVersion)""
    }
}";
            var gpuInfo = RunPowerShellCommand(command);
            DebugHelper.WriteLine("GPU Info: " + gpuInfo);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading GPU info on Windows: " + ex.Message);
        }
    }
    private static string RunPowerShellCommand(string command)
    {
        var process = new Process();
        process.StartInfo.FileName = "powershell";
        process.StartInfo.Arguments = $"-Command \"{command}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
    private static void PrintGraphicsInfoLinux()
    {
        try
        {
            var GpuInfo = RunShellCommand("lspci | grep -i 'vga'");
            if (!string.IsNullOrWhiteSpace(GpuInfo))
            {
                DebugHelper.WriteLine("GPU: " + GpuInfo);
                var glxInfo = RunShellCommand("glxinfo | grep -E 'OpenGL version|OpenGL vendor string'");

                var lines = glxInfo.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                var driverVersion = lines.FirstOrDefault(line => line.Contains("OpenGL version")).Split(':')[1].Trim();
                var vendor = lines.FirstOrDefault(line => line.Contains("OpenGL vendor string")).Split(':')[1].Trim();
                DebugHelper.WriteLine($"Driver Version: {vendor} {driverVersion}");
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading GPU info on Linux: " + ex.Message);
        }

        try
        {
            var gpuInfo = File.ReadAllText("/proc/driver/nvidia/version");
            var firstLine = gpuInfo.Split(Environment.NewLine)[0];
            var match = Regex.Match(firstLine, @"\b(\d+\.\d+\.\d+)\b");
            if (match.Success)
            {
                DebugHelper.WriteLine("NVIDIA GPU Driver Version: " + match.Value);
            }
        }
        catch
        {
            // I acknowledge I am swallowing errors.
        }

        try
        {
            var output = RunShellCommand("xrandr --listmonitors");
            var lines = output.Split('\n');

            // Skip the first line (header)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line) && line.Contains("+"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var monitorName = parts[3];

                    // The coordinates are at the end of the resolution, typically in format +x+y
                    var resolutionAndCoords = parts[2];  // e.g., "1920/344x1080/193+885+1080"
                    var coordinates = resolutionAndCoords.Split('+');
                    var x = coordinates[1];
                    var y = coordinates[2];

                    DebugHelper.WriteLine($"Monitor: {monitorName}, Coordinates: ({x}, {y})");
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading X11 monitor info on Linux: " + ex.Message);
        }
    }

    private static void PrintGraphicsInfoMacOS()
    {
        try
        {
            var output = RunShellCommand("system_profiler SPDisplaysDataType");
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("Chipset Model"))
                {
                    var gpu = line.Split(":")[1].Trim();
                    DebugHelper.WriteLine($"GPU: {gpu}");
                }
                if (line.Contains("Driver Version"))
                {
                    var driverVersion = line.Split(":")[1].Trim();
                    DebugHelper.WriteLine($"Driver Version: {driverVersion}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading GPU info on macOS: " + ex.Message);
        }

        try
        {
            var output = RunShellCommand("system_profiler SPDisplaysDataType");
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("Resolution"))
                {
                    var resolution = line.Split(":")[1].Trim();
                    DebugHelper.WriteLine($"Monitor Resolution: {resolution}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine("Error reading monitor info on macOS: " + ex.Message);
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
        if (OperatingSystem.IsMacOS())
        {
            return CheckMacOSHdr();
        }
        // Detection of HDR on Linux is way too work.
        // If they're on Linux, they should know they're using things like HDR.
        return false;
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
    [SupportedOSPlatform("windows")]
    private static bool CheckWindowsHdr()
    {
        var hdc = GetDC(IntPtr.Zero);
        var bpp = GetDeviceCaps(hdc, BITSPIXEL);
        return bpp >= 30;
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

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    private const int BITSPIXEL = 12;
}
