using System.Reflection;
using SnapX.Core;

namespace SnapX.CommonUI;

public class AboutDialog
{
    public virtual void Show()
    {
        throw new NotImplementedException();
    }

    public virtual string GetSystemInfo()
    {
        return SnapX.Core.Utils.OsInfo.GetFancyOSNameAndVersion();
    }
    public virtual string GetTitle() => Core.SnapX.Title;
    public virtual string GetLicense() => "GPL v3 or Later";

    public virtual string GetLicenseURL() =>
        $"{Core.Utils.Miscellaneous.Links.GitHub}/blob/develop/LICENSE.md";
    public virtual string GetVersion() => Core.SnapX.VersionText;
    public virtual string GetWebsite() => Core.Utils.Miscellaneous.Links.GitHub;
    public virtual string GetDocumentation() => Core.Utils.Miscellaneous.Links.Docs;
    public virtual string GetIssues() => Core.Utils.Miscellaneous.Links.GitHubIssues;
    public virtual string GetDiscord() => Core.Utils.Miscellaneous.Links.Discord;

    public virtual string GetDescription() => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "Image sharing tool";
    public virtual string GetCopyright() =>
        ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute))!).Copyright;
    public virtual string GetRuntime() => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
    public virtual string GetOsPlatform() => $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    public virtual string GetBuildInformation()
    {
        var title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Unknown Title";
        var version = ThisAssembly.AssemblyFileVersion ?? "Unknown Version";
        var flags = string.Join(", ", Core.SnapX.Flags);
        var informationalVersion = ThisAssembly.AssemblyInformationalVersion ?? "Unknown Informational Version";

        string gitCommitId;
        string gitCommitDate;
        string isPrerelease;

        try
        {
            gitCommitId = ThisAssembly.GitCommitId ?? "Unavailable";
        }
        catch
        {
            gitCommitId = "Unavailable";
        }

        try
        {
            gitCommitDate = ThisAssembly.GitCommitDate.ToLongDateString();
        }
        catch
        {
            gitCommitDate = "Unknown Date";
        }

        try
        {
            isPrerelease = ThisAssembly.IsPrerelease.ToString();
        }
        catch
        {
            isPrerelease = "Unknown";
        }

        return $"{title} v{version}{Environment.NewLine}" +
               $"Flags: {flags}{Environment.NewLine}" +
               $"Build: {Core.SnapX.Build}{Environment.NewLine}" +
               $"Informational Version: {informationalVersion} (IsPrerelease: {isPrerelease}){Environment.NewLine}" +
               $"Commit {gitCommitId} ({gitCommitDate})";
    }
    public virtual string GetOsArchitecture() => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();


}
