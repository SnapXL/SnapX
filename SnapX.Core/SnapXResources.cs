
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Runtime.InteropServices;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core;

public static class SnapXResources
{
    public static string CPU => OsInfo.GetProcessorName();
    public static int CPUCount => Environment.ProcessorCount;
    public static OsInfo.GenericGraphicsInfo graphicsInfo => OsInfo.GetGenericGraphicsInfo();
    public static string Dotnet => RuntimeInformation.FrameworkDescription;
    public static string fancyOsName => Helpers.GetOperatingSystemProductName();
    public static string? UserAgent => $"{SnapX.AppName}/{Helpers.GetApplicationVersion()} (+{Links.GitHub})";
}
