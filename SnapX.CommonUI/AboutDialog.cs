using System.Reflection;

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
    public virtual string GetBuildInformation() => $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title} v{ThisAssembly.AssemblyFileVersion}{Environment.NewLine}Flags: {string.Join(", ", Core.SnapX.Flags)}{Environment.NewLine}Build: {Core.SnapX.Build}{Environment.NewLine}Informational Version: {ThisAssembly.AssemblyInformationalVersion} (IsPrerelease: {ThisAssembly.IsPrerelease}){Environment.NewLine}Commit {ThisAssembly.GitCommitId} ({ThisAssembly.GitCommitDate.ToLongDateString()})";
    public virtual string GetOsArchitecture() => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();


}
