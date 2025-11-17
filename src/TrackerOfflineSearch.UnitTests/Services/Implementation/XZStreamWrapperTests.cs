using System.Reactive.Disposables;
using System.Text;
using TrackerOfflineSearch.Services.Implementation;

namespace TrackerOfflineSearch.UnitTests.Services.Implementation;

public sealed class XZStreamWrapperTests : IDisposable
{
    // ----- Base64 fixtures -----
    // printf "Hello" | xz -c | base64 --wrap=0
    private static readonly byte[] _xzHello = Convert.FromBase64String(
        "/Td6WFoAAATm1rRGAgAhARYAAAB0L+WjAQAESGVsbG8AAAAAyKx7yDtcz1EAAR0FuC2Arx+2830BAAAAAARZWg=="
    );

    // printf " " | xz -c | base64 --wrap=0
    private static readonly byte[] _xzSpace = Convert.FromBase64String(
        "/Td6WFoAAATm1rRGAgAhARYAAAB0L+WjAQAAIAAAAADL8wHGA+Oa5AABGQGlLIHMH7bzfQEAAAAABFla"
    );

    // printf "World" | xz -c | base64 --wrap=0
    private static readonly byte[] _xzWorld = Convert.FromBase64String(
        "/Td6WFoAAATm1rRGAgAhARYAAAB0L+WjAQAEV29ybGQAAAAAgcMjRyR6wfQAAR0FuC2Arx+2830BAAAAAARZWg=="
    );

    private readonly CompositeDisposable _disposables = new();

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private string WriteTempFile(byte[] data)
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, data);

        _disposables.Add(
            Disposable.Create(() => {
                File.Delete(path);
            })
        );

        return path;
    }

    [Fact]
    public void CanRead_SingleXzFile_ReturnsDecompressedBytes()
    {
        // Arrange
        var file = WriteTempFile(_xzHello);
        var expected = Encoding.UTF8.GetBytes("Hello");

        using var wrapper = new XZStreamWrapper(file);
        var buffer = new byte[100];

        // Act
        var read = wrapper.Read(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(expected.Length, read);
        Assert.Equal("Hello", Encoding.UTF8.GetString(buffer, 0, read));
    }

    [Fact]
    public void CanReadByte_SingleXz_ReturnsExpectedSequence()
    {
        // Arrange
        var file = WriteTempFile(_xzHello);
        using var wrapper = new XZStreamWrapper(file);

        var result = new MemoryStream();

        int b;
        while ((b = wrapper.ReadByte()) != -1)
        {
            result.WriteByte((byte)b);
        }

        // Assert
        Assert.Equal("Hello", Encoding.UTF8.GetString(result.ToArray()));
    }

    [Fact]
    public void ReadAcrossConcatenatedStreams_Sequentially_ReturnsAll()
    {
        // Arrange: concat XZ streams ("Hello" + " " + "World")
        var combined = new byte[_xzHello.Length + _xzSpace.Length + _xzWorld.Length];
        Buffer.BlockCopy(_xzHello, 0, combined, 0, _xzHello.Length);
        Buffer.BlockCopy(_xzSpace, 0, combined, _xzHello.Length, _xzSpace.Length);
        Buffer.BlockCopy(_xzWorld, 0, combined, _xzHello.Length + _xzSpace.Length, _xzWorld.Length);

        var file = WriteTempFile(combined);

        using var wrapper = new XZStreamWrapper(file);
        var result = new MemoryStream();

        // Act: read until EOF
        var buffer = new byte[1024];
        int read;

        while ((read = wrapper.Read(buffer, 0, buffer.Length)) > 0)
        {
            result.Write(buffer, 0, read);
        }

        // Assert
        Assert.Equal("Hello World", Encoding.UTF8.GetString(result.ToArray()));
    }

    [Fact]
    public void ReadByte_AcrossConcatenatedStreams_WorksCorrectly()
    {
        // Arrange
        // Arrange: concat XZ streams ("Hello" + " " + "World")
        var combined = new byte[_xzHello.Length + _xzSpace.Length + _xzWorld.Length];
        Buffer.BlockCopy(_xzHello, 0, combined, 0, _xzHello.Length);
        Buffer.BlockCopy(_xzSpace, 0, combined, _xzHello.Length, _xzSpace.Length);
        Buffer.BlockCopy(_xzWorld, 0, combined, _xzHello.Length + _xzSpace.Length, _xzWorld.Length);

        var file = WriteTempFile(combined);

        using var wrapper = new XZStreamWrapper(file);
        using var result = new MemoryStream();

        int b;
        while ((b = wrapper.ReadByte()) != -1)
        {
            result.WriteByte((byte)b);
        }

        // Assert
        Assert.Equal("Hello World", Encoding.UTF8.GetString(result.ToArray()));
    }

    [Fact]
    public void Dispose_DisablesReading()
    {
        var file = WriteTempFile(_xzHello);
        var wrapper = new XZStreamWrapper(file);

        wrapper.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            wrapper.ReadExactly(new byte[5], 0, 5);
        });
    }

    [Fact]
    public void Position_ThrowsNotSupported()
    {
        var file = WriteTempFile(_xzHello);
        using var wrapper = new XZStreamWrapper(file);

        Assert.Throws<NotSupportedException>(() => wrapper.Position);
        Assert.Throws<NotSupportedException>(() => wrapper.Position = 0);
    }

    [Fact]
    public void Seek_ThrowsNotSupported()
    {
        var file = WriteTempFile(_xzHello);
        using var wrapper = new XZStreamWrapper(file);

        Assert.Throws<NotSupportedException>(() => wrapper.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void Write_ThrowsNotSupported()
    {
        var file = WriteTempFile(_xzHello);
        using var wrapper = new XZStreamWrapper(file);

        Assert.Throws<NotSupportedException>(() => wrapper.Write(new byte[5], 0, 5));
    }
}

