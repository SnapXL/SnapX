using SnapX.Core.Upload.BaseUploaders;

namespace SnapX.Core.Upload.File;

public abstract class FtpBase(FTPAccount Account) : FileUploader, IDisposable
{

    public FTPAccount Account { get; protected set; } = Account;

    public abstract bool IsConnected { get; }

    public abstract bool Connect();

    public abstract void Disconnect();

    public abstract override UploadResult Upload(Stream stream, string? fileName);

    public abstract override void StopUpload();

    public abstract void Dispose();
}
