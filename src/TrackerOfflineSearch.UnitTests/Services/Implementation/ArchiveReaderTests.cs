using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Implementation;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.UnitTests.Services.Implementation;

public class ArchiveReaderTests
{
    private readonly Mock<ILogger<ArchiveReader>> _loggerMock = new();
    private readonly Mock<IXmlStreamFactory> _streamFactoryMock = new();

    private readonly ArchiveReader _reader;

    public ArchiveReaderTests()
    {
        _reader = new ArchiveReader(_loggerMock.Object, _streamFactoryMock.Object);
    }

    // UTF-8 XML stream
    private static Stream MakeStream(string xml) =>
        new MemoryStream(Encoding.UTF8.GetBytes(xml));

    [Fact]
    public async Task ReadPostsAsync_SinglePost_ReturnsOne()
    {
        const string xml = """
            <root>
              <torrent id="123" registred_at="2024.01.1 10:00:00" size="100">
                <title>Hello</title>
                <torrent hash="abc" tracker_id="5" />
                <forum id="1">Movies</forum>
                <content>Body</content>
              </torrent>
            </root>
        """;

        await using var stream = MakeStream(xml);
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        var result = new List<Post>();
        await foreach (var p in _reader.ReadPostsAsync("dummy", skipContent: false, CancellationToken.None))
        {
            result.Add(p);
        }

        Assert.Single(result);
        Assert.Equal(123, result[0].Id);
        Assert.Equal("Hello", result[0].Title);
        Assert.Equal("Body", result[0].Content);
        Assert.Equal("Movies", result[0].ForumName);
    }

    [Fact]
    public async Task ReadPostsAsync_MultiplePosts_ReturnsAll()
    {
        const string xml = """
            <root>
              <torrent id="1" registred_at="2024.01.1 10:00:00" size="10">
                <title>A1</title>
                <torrent hash="h1" tracker_id="1" />
                <forum id="1">F1</forum>
                <content>C1</content>
              </torrent>
              <torrent id="2" registred_at="2024.01.1 10:00:00" size="20">
                <title>B1</title>
                <torrent hash="h2" tracker_id="2" />
                <forum id="2">F2</forum>
                <content>C2</content>
              </torrent>
            </root>
        """;

        await using var stream = MakeStream(xml);
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        var posts = new List<Post>();
        await foreach (var p in _reader.ReadPostsAsync("p", skipContent: false, CancellationToken.None))
        {
            posts.Add(p);
        }

        Assert.Equal(2, posts.Count);
        Assert.Equal(1, posts[0].Id);
        Assert.Equal(2, posts[1].Id);
    }

    [Fact]
    public async Task ReadPostsAsync_SkipContent_ContentIsEmpty()
    {
        const string xml = """
            <root>
              <torrent id="7" registred_at="2024.01.1 10:00:00" size="50">
                <title>A</title>
                <torrent hash="h" tracker_id="1"/>
                <forum id="1">F</forum>
                <content>ShouldNotAppear</content>
              </torrent>
            </root>
        """;

        await using var stream = MakeStream(xml);
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        Post? result = null;
        await foreach (var p in _reader.ReadPostsAsync("p", skipContent: true, CancellationToken.None))
        {
            result = p;
        }

        Assert.NotNull(result);
        Assert.Equal("", result!.Content);
    }

    [Fact]
    public async Task ReadPostsAsync_MappingError_LogsAndSkipPost()
    {
        const string xml = """
            <root>
              <!-- Missing required attributes => mapping fails -->
              <torrent>
                <title>A</title>
                <torrent hash="h" tracker_id="1"/>
                <forum id="1">F</forum>
                <content>Body</content>
              </torrent>
            </root>
        """;

        await using var stream = MakeStream(xml);
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        var posts = new List<Post>();
        await foreach (var p in _reader.ReadPostsAsync("p", false, CancellationToken.None))
        {
            posts.Add(p);
        }

        // Should return empty result
        Assert.Empty(posts);

        // Logger should have logged an error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReadPostsAsync_NoPosts_ReturnsEmpty()
    {
        const string xml = "<root><nothing /></root>";

        await using var stream = MakeStream(xml);
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        var count = 0;
        await foreach (var _ in _reader.ReadPostsAsync("p", false, CancellationToken.None))
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadPostsAsync_CancellationRequested_StopsImmediately()
    {
        const string xml = """
            <root>
              <torrent id="1" registred_at="2024.01.1 10:00:00" size="10">
                <title>A</title>
                <torrent hash="h" tracker_id="1"/>
                <forum id="1">F</forum>
                <content>C</content>
              </torrent>
              <torrent id="2" registred_at="2024.01.1 10:00:00" size="20">
                <title>B</title>
                <torrent hash="h2" tracker_id="2"/>
                <forum id="2">F2</forum>
                <content>C2</content>
              </torrent>
            </root>
        """;

        await using var stream = MakeStream(xml);
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        var cts = new CancellationTokenSource();
        var results = new List<Post>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await foreach (var p in _reader.ReadPostsAsync("x", false, cts.Token))
            {
                results.Add(p);
                cts.Cancel(); // stop after first
            }
        });

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public async Task ReadPostsAsync_DisposesStream()
    {
        const string xml = "<root></root>";

        //await using var stream = MakeStream(xml);
        await using var stream = new TestStream(Encoding.UTF8.GetBytes(xml));
        _streamFactoryMock.Setup(f => f.GetStream(It.IsAny<string>())).Returns(stream);

        await foreach (var _ in _reader.ReadPostsAsync("p", false, CancellationToken.None))
        {
        }

        Assert.True(stream.Disposed);
    }

    private sealed class TestStream(byte[] buffer) : MemoryStream(buffer)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            Disposed = true;
            return base.DisposeAsync();
        }
    }
}
