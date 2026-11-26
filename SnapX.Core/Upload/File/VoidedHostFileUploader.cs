
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Img;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;

public class VoidedHostFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.VoidedHost;

    public override bool CheckConfig(UploadersConfig config) => VoidedHostUploader.IsUploadConfigured(config);

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        bool useGuest = VoidedHostUploader.ShouldUseGuestMode(config);
        string key = VoidedHostUploader.GetEffectiveUploadKey(config);

        return new VoidedHostFileUploader(key, useGuest);
    }
}

public sealed class VoidedHostFileUploader : FileUploader
{
    private readonly VoidedHostMultipartUploader _multipart;

    public VoidedHostFileUploader(string uploadKey, bool useGuestMode)
    {
        _multipart = new VoidedHostMultipartUploader(uploadKey, useGuestMode, VoidedHostUploader.FileUploadApiUrl);
        _multipart.ProgressChanged += OnProgressChanged;
        _multipart.EarlyURLCopyRequested += OnEarlyURLCopyRequested;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        Errors.Errors.Clear();
        _multipart.BufferSize = BufferSize;
        UploadResult result = _multipart.Upload(stream, fileName);
        Errors.Add(_multipart.Errors);
        return result;
    }

    public override void StopUpload()
    {
        _multipart.StopUpload();
        base.StopUpload();
    }
}
