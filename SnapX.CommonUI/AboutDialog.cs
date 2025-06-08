using System.Diagnostics.CodeAnalysis;
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ThisAssembly))]
    [RequiresUnreferencedCode("Uses reflection to access properties that may be removed by the trimmer.")]
    public virtual string GetBuildInformation()
    {
        var title = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Unknown Title";
        var flags = string.Join(", ", Core.SnapX.Flags);
        var informationalVersion = ThisAssembly.AssemblyInformationalVersion;

        var type = typeof(ThisAssembly);
        var gitCommitId = type.GetProperty("GitCommitId")?.GetValue(null) as string ?? "Unavailable";
        var gitCommitDateValue = type.GetProperty("GitCommitDate")?.GetValue(null);
        var isPrerelease = type.GetProperty("IsPrerelease")?.GetValue(null)?.ToString() ?? "Unknown";

        var gitCommitDate = gitCommitDateValue is DateTime dt
            ? dt.ToLongDateString()
            : "Unknown Date";

        return $"{title} v{ThisAssembly.AssemblyFileVersion}{Environment.NewLine}" +
               $"Flags: {flags}{Environment.NewLine}" +
               $"Build: {Core.SnapX.Build}{Environment.NewLine}" +
               $"Informational Version: {informationalVersion} (IsPrerelease: {isPrerelease}){Environment.NewLine}" +
               $"Commit {gitCommitId} ({gitCommitDate})";
    }
    public virtual string GetOsArchitecture() => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();


}
