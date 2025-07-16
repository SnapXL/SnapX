// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Utils.Miscellaneous;

internal sealed class MaxLengthStream : Stream
{
    private readonly Stream stream;
    private long length = 0L;

    public MaxLengthStream(Stream stream, long maxLength)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        MaxLength = maxLength;
    }

    public long MaxLength { get; }

    public override bool CanRead => stream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => stream.Length;

    public override long Position
    {
        get => stream.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int result = stream.Read(buffer, offset, count);
        length += result;
        if (length > MaxLength)
        {
            throw new Exception("Stream is larger than the maximum allowed size.");
        }

        return result;
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        stream.Dispose();
        base.Dispose(disposing);
    }
}

