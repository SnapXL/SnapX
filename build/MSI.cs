using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
// WixSharp is Windows-only
#if WINDOWS
using MarkdownToRtf;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
using WixSharp.UI;
using WixToolset.Dtf.WindowsInstaller;
#endif
using File = System.IO.File;

namespace DefaultNamespace;

#pragma warning disable CS9113 // Parameter is unread.
public class MSI(IBuildLogger Logger, CommandRunner CommandRunner, FS FileSystem, BuildConfig config)
#pragma warning restore CS9113 // Parameter is unread.
{
    public async Task ProcessMSI(bool signBinaries = false, bool generateMSIX = false)
    {
#if WINDOWS
        var platform = Platform.x64;
        if (config.Runtime.Contains("x64", StringComparison.OrdinalIgnoreCase))
            platform = Platform.x64;
        else if (config.Runtime.Contains("arm64", StringComparison.OrdinalIgnoreCase))
            platform = Platform.arm64;
        else if (config.Runtime.Contains("arm", StringComparison.OrdinalIgnoreCase))
            platform = Platform.arm;
        else if (config.Runtime.Contains("x86", StringComparison.OrdinalIgnoreCase))
            platform = Platform.x86;
        var thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var product = thisAssembly
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product ?? string.Empty;
        var manufacturer = "SnapXL";
        var repo = $"https://github.com/{manufacturer}/{product}";
        var RTFReadmeContents =
            MarkdownToRtfConverter.Convert(
                await File.ReadAllTextAsync(Path.Combine(config.RootDirectory, "README.md")));
        var RTFLicenseContents =
            MarkdownToRtfConverter.Convert(
                await File.ReadAllTextAsync(Path.Combine(config.RootDirectory, "LICENSE.md")));
        var RTFLicensePath = Path.Combine(config.PackagingDirectory, "LICENSE.rtf");
        var RTFReadmePath = Path.Combine(config.PackagingDirectory, "README.rtf");
        var buildNumberSearch = new Property("WIN10BUILD",
            new RegistrySearch(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "CurrentBuildNumber",
                RegistrySearchType.raw));
        var winVersionCondition = new LaunchCondition(
            "Installed OR (WIN10BUILD >= \"19045\")",
            "This application requires Windows 10.0.19045 (22H2) or newer."
        );
        await File.WriteAllTextAsync(RTFReadmePath, RTFReadmeContents);
        await File.WriteAllTextAsync(RTFLicensePath, RTFLicenseContents);
        var disableTelemetry = new Feature("Disable Telemetry")
        {
            Description = "Anonymous usage data helps us squash bugs and optimize performance. We don't track who you are, just what breaks. Turn this off if you'd rather we fly blind.",
            IsEnabled = false,
            AllowChange = true,
            Condition = new FeatureCondition("DISABLE_TELEMETRY = \"1\"", level: 2)
        };
        var telemetryReg = new RegValue(
            disableTelemetry,
            RegistryHive.LocalMachine,
            $@"SOFTWARE\{manufacturer}\{product}",
            "DisableTelemetry",
            "true");
        var startMenuDir = new Dir($@"%ProgramMenu%\{manufacturer}");

        var startMenuShortcut = new FileShortcut(product, startMenuDir.Name);
        var desktopShortcut = new FileShortcut(product, @"%Desktop%");
        var mainInstallDir = new InstallDir(
            new Id("APPLICATIONFOLDER"),
            @$"%ProgramFiles64%\{manufacturer}\{product}",

            new WixSharp.File(Path.Combine(config.OutputDir, config.TargetInstallAssembly ?? "snapx-ui", "snapx-ui.exe"),
                startMenuShortcut,
                desktopShortcut),

            new Files(Path.Join(config.OutputDir, config.TargetInstallAssembly ?? "snapx-ui") + $"{Path.DirectorySeparatorChar}**",
                f => !f.EndsWith("snapx-ui.exe"))
        );
        var project =
            new Project(product,
                startMenuDir,
                mainInstallDir,
                // new ManagedAction(Actions.CustomAction),
                // new ManagedAction(Actions.CustomAction2),
                new Property("WixAppFolder", "WixPerMachineFolder"),
                new Property("ApplicationFolderName", product),
                new Property("DISABLE_TELEMETRY", "0"),
                new Property("LicenseAccepted", "1"),
                new Property("DISABLE_TELEMETRY_STR", "false"),
                telemetryReg)
            {
                Platform = platform,
                Description = thisAssembly
                    .GetCustomAttribute<AssemblyDescriptionAttribute>()?
                    .Description ?? string.Empty,
                ControlPanelInfo =
                {
                    HelpLink = $"{repo}/issues",
                    UrlInfoAbout = repo,
                    Contact = "brycengranville+snapx.cpi@outlook.com",
                    Manufacturer = manufacturer,
                    ProductIcon = Path.Combine(config.ProjectsToBuild[0], "Assets", $"{product}_Icon.ico" ),
                    Readme = RTFReadmeContents
                },
                LicenceFile = RTFLicensePath,
                BannerImage = Path.Combine(config.PackagingDirectory, "banner.bmp"),
                SignAllFiles = signBinaries
                // Probably not a good idea to put Linux branding in MSI, right? lmfao
                // BackgroundImage = Path.Combine(config.PackagingUsrDir,  "share", "icons", "hicolor", "256x256", "apps",  "io.github.SnapXL.SnapX.png" ),
            };
        project.AddProperty(buildNumberSearch);
        project.LaunchConditions.Add(winVersionCondition);
        if (!generateMSIX)
        {
            project.CustomUI = new DialogSequence()
                .On(NativeDialogs.WelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.InstallDirDlg))

                .On(NativeDialogs.InstallDirDlg, Buttons.Back, new ShowDialog(NativeDialogs.WelcomeDlg))

                .On(NativeDialogs.InstallDirDlg, Buttons.Next, new ShowDialog(NativeDialogs.VerifyReadyDlg));
        }

        project.Version = thisAssembly.GetName().Version;
        // USeful for debugging
        // project.PreserveTempFiles = true;

        var version = config.SnapXVersion;
        var arch = RuntimeInformation.ProcessArchitecture.ToString();
        var uname = config.Uname;
        var edition = config.Edition;
        var suffix = !string.IsNullOrEmpty(edition) ? $"-{edition}" : "";
        project.OutFileName = $"SnapX{suffix}-{config.Configuration}-{uname}-{version}-{arch}";
        project.OutDir = config.PackagingDirectory;
        project.UI = generateMSIX ? WUI.WixUI_ProgressOnly : WUI.WixUI_Advanced;

        var msi = project.BuildMsi();
        if (!generateMSIX) return;
        var msixXMLFile = Path.Combine(config.PackagingDirectory, $"{product}.msix.xml");
        project.UpdateTemplate(msixXMLFile, msi);

        if (CommandRunner.IsAdmin())
        {
            msi.ConvertToMsix(msixXMLFile);
        }
        else
        {
            throw new InvalidOperationException("Error: you need run the build process as Administrator if you want to build the MSIX setup.");
        }
#else
        Logger.Warning("MSI/MSIX generation is only supported on Windows. Skipping step.");
        await Task.CompletedTask;
#endif
    }
}
#if WINDOWS
internal static class Msix
{
    extension(Project project)
    {
        public void UpdateTemplate(string msixTemplate, string msi)
        {
            XNamespace ns = "http://schemas.microsoft.com/msix/msixpackagingtool/template/1910";

            var doc = XDocument.Load(msixTemplate);

            doc.Root.FindFirst("SaveLocation")
                .SetAttribute("PackagePath", msi.PathChangeExtension(".msix"))
                .SetAttribute("TemplatePath", msixTemplate.PathChangeExtension(".g.xml"));

            doc.Root.FindFirst("Installer")
                .SetAttribute("Path", msi)
                .SetAttribute("InstallLocation", @$"C:\Program Files\{project.ControlPanelInfo.Manufacturer}\{project.Name}");
            doc.Root.FindFirst("PackageInformation")
                .SetAttribute("PackageName", project.Name)
                .SetAttribute("PackageDisplayName", project.Name)
                .SetAttribute(ns + "PackageDescription", project.Name)
                .SetAttribute("PublisherName", project.SignAllFiles ? "CN=" + project.ControlPanelInfo.Manufacturer : "CN=AppModelSamples, OID.2.25.311729368913984317654407730594956997722=1")
                .SetAttribute("PublisherDisplayName", project.ControlPanelInfo.Manufacturer)
                .SetAttribute("Version", project.Version);

            doc.Save(msixTemplate);
        }
    }

    extension(string msi)
    {
        public void ConvertToMsix(string msixTemplate)
        {
            // Note MsixPackagingTool builds msix by installing msi and analyzing system changes and then embedding detected
            // changes (e.g. files) in the produced msix.
            // Thus, it is important to clean up the system after the msi installation.

            using var msiInfo = new MsiParser(msi);
            var productCode = msiInfo.GetProductCode();

            if (MsiParser.IsInstalled(productCode))
                "msiexec".Run("/x " + productCode + " /q");

            var startInfo = new ProcessStartInfo
            {
                FileName = @"MsixPackageTool.exe",
                Arguments = @"create-package --template " + msixTemplate, //  use "-v" for more detailed build output
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(startInfo);
                while (process?.StandardOutput.ReadLine() is { } line)
                    Console.WriteLine(line);

                var error = process?.StandardError.ReadToEnd();
                if (!error.IsEmpty())
                    Console.WriteLine(error);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message +
                                  ". Ensure you have installed MsixPackagingTool and MSIX driver. If you're struggling to use this tool, you don't have to. MSIX Packaging Tool can convert a MSI into a MSIX trivially.");
            }
            finally
            {
                if (MsiParser.IsInstalled(productCode))
                    "msiexec".Run("/x " + productCode + " /q");
            }
        }
    }
}

public class Actions
{
    [CustomAction]
    public static ActionResult CustomAction(Session session)
    {
        Native.MessageBox("MSI Session\nINSTALLDIR: " + session.Property("INSTALLDIR"), "WixSharp - .NET9");

        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult CustomAction2(Session session)
    {
        var args = session.ToEventArgs();

        Native.MessageBox("WixSharp RuntimeData\nMsiFile: " + args.MsiFile, "WixSharp - .NET9");

        return ActionResult.UserExit; // terminate the setup
    }
}
#endif
