
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload;

public static partial class UploaderFactory
{
    public static List<IUploaderService> AllServices { get; } = [];
    public static List<IGenericUploaderService> AllGenericUploaderServices { get; } = [];
    public static Dictionary<ImageDestination, ImageUploaderService> ImageUploaderServices { get; } = CacheServices<ImageDestination, ImageUploaderService>();
    public static Dictionary<TextDestination, TextUploaderService> TextUploaderServices { get; } = CacheServices<TextDestination, TextUploaderService>();
    public static Dictionary<FileDestination, FileUploaderService> FileUploaderServices { get; } = CacheServices<FileDestination, FileUploaderService>();
    public static Dictionary<UrlShortenerType, URLShortenerService> URLShortenerServices { get; } = CacheServices<UrlShortenerType, URLShortenerService>();
    public static Dictionary<URLSharingServices, URLSharingService> URLSharingServices { get; } = CacheServices<URLSharingServices, URLSharingService>();

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Helpers.GetInstances<T2>() uses reflection; required members preserved via attribute"
    )]
    [RequiresUnreferencedCode("Uses reflection to instantiate and analyze T2 types.")]
    private static Dictionary<T, T2> CacheServices<T,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    T2>()
        where T2 : UploaderService<T> where T : notnull
    {
        var instances = Helpers.GetInstances<T2>();

        AllServices.AddRange(instances.OfType<IUploaderService>());
        AllGenericUploaderServices.AddRange(instances.OfType<IGenericUploaderService>());

        return instances.ToDictionary(x => x.EnumValue, x => x);
    }
}

