using SharpCompress.Compressors.Xz;

namespace TrackerOfflineSearch.Core.Services;

/// <summary>
/// Stream wrapper that transparently decompresses one or more concatenated XZ streams from a file.
/// Supports sequential read-only access only.
/// </summary>
public class XZStreamWrapper : Stream
{
    public XZStreamWrapper(string path)
    {
        _fileStream = File.OpenRead(path);
        _decompressor = new XZStream(_fileStream);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _decompressor?.Dispose();
                _decompressor = null;
                _fileStream.Dispose();
            }
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    #region Stream overrides

    public override bool CanRead => !_disposed;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush()
    {
        // No-op: this is a read-only stream.
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        CheckDisposed();

        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset + count > buffer.Length)
        {
            throw new ArgumentException("Invalid offset and length combination");
        }

        int totalBytesRead = 0;

        while (count > 0 && _decompressor is not null)
        {
            int bytesRead = _decompressor.Read(buffer, offset, count);

            if (bytesRead > 0)
            {
                totalBytesRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }
            else
            {
                // End of current XZ stream: try to move to the next concatenated stream.
                _decompressor = RebuildDecompressor();
                if (_decompressor is null)
                {
                    break;
                }
            }
        }

        return totalBytesRead;
    }

    public override int ReadByte()
    {
        CheckDisposed();

        if (_decompressor is null)
        {
            return -1;
        }

        int result = _decompressor.ReadByte();

        if (result == -1)
        {
            // End of current stream: rebuild and try the next one.
            _decompressor = RebuildDecompressor();
            if (_decompressor is not null)
            {
                result = _decompressor.ReadByte();
            }
        }

        return result;
    }

    #endregion

    #region Private

    private void CheckDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Disposes the current decompressor and creates a new one
    /// if more data is available in the underlying file stream.
    /// </summary>
    private XZStream? RebuildDecompressor()
    {
        _decompressor?.Dispose();

        if (_fileStream.Position >= _fileStream.Length)
        {
            return null;
        }

        return new XZStream(_fileStream);
    }

    private readonly FileStream _fileStream;
    private bool _disposed = false;

    private XZStream? _decompressor;

    #endregion
}
