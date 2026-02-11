namespace SnapX.Core.Utils.Miscellaneous;

public class ProgressReadStream(Stream InnerStream, Action<long> OnProgress) : Stream
{
    private long _totalRead;

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await InnerStream.ReadAsync(buffer, cancellationToken);
        if (read > 0)
        {
            _totalRead += read;
            OnProgress(_totalRead);
        }
        return read;
    }

    public override bool CanRead => InnerStream.CanRead;
    public override bool CanSeek => InnerStream.CanSeek;
    public override bool CanWrite => InnerStream.CanWrite;
    public override long Length => InnerStream.Length;
    public override long Position { get => InnerStream.Position; set => InnerStream.Position = value; }
    public override void Flush() => InnerStream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => InnerStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);
    public override void SetLength(long value) => InnerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => InnerStream.Write(buffer, offset, count);
}
