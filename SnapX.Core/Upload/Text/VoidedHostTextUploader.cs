
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Img;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.Text;

public class VoidedHostTextUploaderService : TextUploaderService
{
    public override TextDestination EnumValue => TextDestination.VoidedHost;

    public override bool CheckConfig(UploadersConfig config) => VoidedHostUploader.IsUploadConfigured(config);

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        bool useGuest = VoidedHostUploader.ShouldUseGuestMode(config);
        string key = VoidedHostUploader.GetEffectiveUploadKey(config);

        return new VoidedHostTextUploader(key, useGuest);
    }
}

public sealed class VoidedHostTextUploader : TextUploader
{
    private readonly VoidedHostMultipartUploader _multipart;

    public VoidedHostTextUploader(string uploadKey, bool useGuestMode)
    {
        _multipart = new VoidedHostMultipartUploader(uploadKey, useGuestMode, VoidedHostUploader.PasteUploadApiUrl);
        _multipart.ProgressChanged += OnProgressChanged;
        _multipart.EarlyURLCopyRequested += OnEarlyURLCopyRequested;
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult UploadText(string? text, string? fileName)
    {
        Errors.Errors.Clear();
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "paste.txt";
        }
        else if (!Path.HasExtension(fileName))
        {
            fileName += ".txt";
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text ?? ""));
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
