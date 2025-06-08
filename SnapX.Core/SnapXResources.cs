
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core;

public static class SnapXResources
{
    public static string UserAgent => $"{SnapX.AppName}/{Helpers.GetApplicationVersion()} (+{Links.GitHub})";
}

