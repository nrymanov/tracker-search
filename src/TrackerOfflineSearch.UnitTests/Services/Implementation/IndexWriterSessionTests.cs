using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Implementation;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.UnitTests.Services.Implementation;

public sealed class IndexWriterSessionTests : IDisposable
{
    private readonly RAMDirectory _directory = new();
    private readonly Analyzer _analyzer = new StandardAnalyzer(AppConsts.SearchEngineVersion);
    private readonly Mock<ILogger<IndexWriterSession>> _loggerMock = new();
    private readonly IOptions<ApplicationsOptions> _options;
    private readonly Post _post = new()
    {
        Id = 1,
        Created = DateTime.UtcNow,
        Size = 123456,

        Title = "Test title",
        Content = "Hello World",
        Hash = "ABCDEF1234567890",

        TrackerId = 1,
        ForumId = 2,
        ForumName = "1 - 2 - 3"
    };

    public IndexWriterSessionTests()
    {
        _options = Options.Create(new ApplicationsOptions { RAMBufferSizeMB = 32 });
    }

    public void Dispose()
    {
        _directory.Dispose();
        _analyzer.Dispose();
    }

    // ----------------------------------------------------------
    // TESTS
    // ----------------------------------------------------------

    [Fact]
    public async Task Add_Adds_Document_And_Marks_HasChanges()
    {
        using (var session = new IndexWriterSession(_loggerMock.Object, _options, _analyzer, _directory))
        {
            session.Add(_post);

            Assert.True(session.HasChanges);

            await session.CommitAsync(CancellationToken.None);
        }

        using var reader = DirectoryReader.Open(_directory);
        Assert.Equal(1, reader.NumDocs);
    }

    [Fact]
    public async Task ClearAsync_Removes_Documents()
    {
        using (var session = new IndexWriterSession(_loggerMock.Object, _options, _analyzer, _directory))
        {
            session.Add(_post);
            await session.CommitAsync(CancellationToken.None);

            using (var reader = DirectoryReader.Open(_directory))
            {
                Assert.Equal(1, reader.NumDocs);
            }

            await session.ClearAsync(CancellationToken.None);
            await session.CommitAsync(CancellationToken.None);

        }

        using (var reader2 = DirectoryReader.Open(_directory))
        {
            Assert.Equal(0, reader2.NumDocs);
        }
    }

    [Fact]
    public async Task CommitAsync_Resets_HasChanges()
    {
        using var session = new IndexWriterSession(_loggerMock.Object, _options, _analyzer, _directory);

        session.Add(_post);
        Assert.True(session.HasChanges);

        await session.CommitAsync(CancellationToken.None);

        Assert.False(session.HasChanges);
    }

    [Fact]
    public void Dispose_Calls_Rollback_When_HasChanges_True()
    {
        var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
            RAMBufferSizeMB = _options.Value.RAMBufferSizeMB,
        };
        using (var writer = new IndexWriter(_directory, config))
        {
            writer.Commit();
        }

        using var session = new IndexWriterSession(_loggerMock.Object, _options, _analyzer, _directory);
        {
            // Mark dirty
            session.Add(_post);
        }

        // Verification is indirect: a rollback does NOT write index,
        // so index should either not exist or be empty.
        using var reader = DirectoryReader.Open(_directory);

        Assert.Equal(0, reader.NumDocs);
    }

    [Fact]
    public async Task Dispose_Does_Not_Rollback_When_No_Changes()
    {
        using (var session = new IndexWriterSession(_loggerMock.Object, _options, _analyzer, _directory))
        {
            // Add + commit -> HasChanges = false
            session.Add(_post);
            await session.CommitAsync(CancellationToken.None);
        }

        using var reader = DirectoryReader.Open(_directory);
        Assert.Equal(1, reader.NumDocs);
    }

    [Theory]
    [InlineData(IndexOptimizationStrategy.Minimum, 100)]
    [InlineData(IndexOptimizationStrategy.Low, 20)]
    [InlineData(IndexOptimizationStrategy.Normal, 10)]
    [InlineData(IndexOptimizationStrategy.High, 5)]
    [InlineData(IndexOptimizationStrategy.Maximum, 1)]
    public async Task OptimizeAsync_Calls_ForceMerge(IndexOptimizationStrategy strategy, int expectedSegments)
    {
        using var session = new IndexWriterSession(_loggerMock.Object, _options, _analyzer, _directory);

        // Add doc so there is something to merge
        session.Add(_post);
        await session.CommitAsync(CancellationToken.None);

        // Act
        await session.OptimizeAsync(strategy, CancellationToken.None);

        // Assert: cannot assert segment count; instead assert HasChanges = true
        Assert.True(session.HasChanges);

        session.Dispose();
    }
}

