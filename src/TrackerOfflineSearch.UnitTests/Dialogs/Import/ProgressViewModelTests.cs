using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using TrackerOfflineSearch.Dialogs.Import;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Models;
using static Lucene.Net.Util.Fst.Util;

namespace TrackerOfflineSearch.UnitTests.Dialogs.Import;

public class ProgressViewModelTests
{
    private readonly Mock<IScreen> _screen = new();
    private readonly Mock<IArchiveReader> _archiveReader = new();
    private readonly Mock<IIndexService> _indexService = new();
    private readonly Mock<IIndexWriterSession> _writerSession = new();

    //private readonly ProgressViewModel _vm;
    private readonly ImportParameters _importParams = new("path", SimpleIndex: false, IndexOptimization: IndexOptimizationStrategy.Normal);

    //public ProgressViewModelTests()
    //{
    //    _vm = new ProgressViewModel(_screen.Object, _archiveReader.Object, _indexService.Object, () => _writerSession.Object);
    //}

    //    private ProgressViewModel CreateVm(IScreen screen = null)
    //    {
    //        _writerSessionFactory.Setup(f => f()).Returns(_writerSession.Object);

    //        var scr = screen ?? Mock.Of<IScreen>();

    //        return new ProgressViewModel(
    //            scr,
    //            _archiveReader.Object,
    //            _indexService.Object,
    //            _writerSessionFactory.Object
    //        );
    //    }

    //    private ImportParameters SampleParams => new("path", SimpleIndex: false, IndexOptimization: TrackerOfflineSearch.Services.Models.IndexOptimizationStrategy.Normal);

    //    /// <summary>
    //    /// Helper: returns an empty async enumerable of Post.
    //    /// </summary>
    //    private static async IAsyncEnumerable<Post> EmptyAsync()
    //    {
    //        await Task.Yield();
    //        yield break;
    //    }

    //    /// <summary>
    //    /// Helper: returns a single post (or empty if null).
    //    /// </summary>
    //    private static async IAsyncEnumerable<Post> SinglePostAsync(Post? p)
    //    {
    //        if (p is not null)
    //        {
    //            yield return p;
    //        }
    //        await Task.Yield();
    //    }

    [Fact]
    public void WhenActivated_TriggersImportCommand()
    {
        new TestScheduler().With(s =>
        {
            var vm = new ProgressViewModel(_screen.Object, _archiveReader.Object, _indexService.Object, () => _writerSession.Object)
                .WithParameters(_importParams);

            bool executed = false;
            vm.ImportCommand.Subscribe(_ => executed = true);

            vm.Activator.Activate();

            s.AdvanceBy(2);

            Assert.True(executed);
        });
    }

    [Fact]
    public void ImportAsync_CompletesSuccessfully()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            _writerSession.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _writerSession.Setup(s => s.OptimizeAsync(It.IsAny<IndexOptimizationStrategy>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _writerSession.Setup(s => s.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Archive returns no documents
            _archiveReader.Setup(r => r.ReadPostsAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            )).Returns(AsyncEnumerable.Empty<Post>());

            var vm = new ProgressViewModel(_screen.Object, _archiveReader.Object, _indexService.Object, () => _writerSession.Object)
                .WithParameters(_importParams);

            ImportResult? result = null;

            vm.ImportCommand.Subscribe(r => result = r);

            vm.Activator.Activate();

            while (result is null)
            {
                s.AdvanceBy(1);
            }

            //s.Start();
            //s.AdvanceBy(50);

            _writerSession.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
            _archiveReader.Verify(r => r.ReadPostsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            _writerSession.Verify(s => s.OptimizeAsync(It.IsAny<IndexOptimizationStrategy>(), It.IsAny<CancellationToken>()), Times.Once);
            _writerSession.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _indexService.Verify(i => i.Refresh(), Times.Once);

            Assert.IsType<ImportCompletedResult>(result);
            _indexService.Verify(s => s.Refresh(), Times.Once);
        });
    }

    [Fact]
    public void ImportAsync_ReturnsFailedResult_OnError()
    {
        new TestScheduler().With(s =>
        {
            _writerSession.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var vm = new ProgressViewModel(_screen.Object, _archiveReader.Object, _indexService.Object, () => _writerSession.Object)
                .WithParameters(_importParams);

            ImportResult? result = null;
            vm.ImportCommand.Subscribe(r => result = r);

            vm.Activator.Activate();

            s.AdvanceBy(2);

            Assert.IsType<ImportFailedResult>(result);
        });
    }

    //    /// <summary>
    //    /// Message property should receive correct progress updates.
    //    /// </summary>
    //    [Fact]
    //    public async Task MessageProperty_IsUpdatedDuringImport()
    //    {
    //        var dummyPost = new Post
    //        {
    //            Id = 1,
    //            Title = "t",
    //            Content = "c",
    //            ForumName = "f",
    //            Index = 0,
    //            Size = 0,
    //            Created = DateTime.UtcNow
    //        };

    //        _archiveReader.Setup(r => r.ReadPostsAsync(
    //            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()
    //        )).Returns(SinglePostAsync(dummyPost));

    //        _writerSession.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    //        _writerSession.Setup(s => s.Add(It.IsAny<Post>()));
    //        _writerSession.Setup(s => s.OptimizeAsync(It.IsAny<IndexOptimizationStrategy>(), It.IsAny<CancellationToken>()))
    //                      .Returns(Task.CompletedTask);
    //        _writerSession.Setup(s => s.CommitAsync(It.IsAny<CancellationToken>()))
    //                      .Returns(Task.CompletedTask);

    //        var vm = CreateVm().WithParameters(SampleParams);

    //        // Act
    //        await vm.ImportCommand.Execute(SampleParams);

    //        // Assert (final message)
    //        Assert.Equal("Импорт завершен", vm.Message);
    //    }

    //    /// <summary>
    //    /// Elapsed counter should increment once per second while ImportCommand is running.
    //    /// </summary>
    //    [Fact]
    //    public void Elapsed_IncrementsCorrectly_WithTestScheduler()
    //    {
    //        new TestScheduler().With(s =>
    //        {
    //            // Delay import so it runs during scheduler time
    //            _writerSession.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>()))
    //                .Returns(async () =>
    //                {
    //                    await Task.Delay(3000, CancellationToken.None);
    //                });

    //            _archiveReader.Setup(r => r.ReadPostsAsync(
    //                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()
    //            )).Returns(AsyncEnumerable.Empty<Post>());

    //            var vm = CreateVm();
    //            vm.WithParameters(SampleParams);

    //            vm.Activator.Activate();

    //            // Start import
    //            vm.ImportCommand.Execute(SampleParams).Subscribe();

    //            s.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
    //            Assert.Equal(TimeSpan.FromSeconds(1), vm.Elapsed);

    //            s.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
    //            Assert.Equal(TimeSpan.FromSeconds(2), vm.Elapsed);
    //        });
    //    }

    //    /// <summary>
    //    /// InfoTip should rotate through tips every 10 seconds.
    //    /// </summary>
    //    [Fact]
    //    public void InfoTip_ChangesEvery10Seconds()
    //    {
    //        new TestScheduler().With(s =>
    //        {
    //            _writerSession.Setup(s => s.ClearAsync(It.IsAny<CancellationToken>()))
    //                .Returns(Task.Delay(20000));

    //            var vm = CreateVm().WithParameters(SampleParams);
    //            vm.Activator.Activate();

    //            vm.ImportCommand.Execute(SampleParams).Subscribe();

    //            var first = vm.InfoTip;

    //            s.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
    //            var second = vm.InfoTip;

    //            Assert.NotEqual(first, second);
    //        });
    //    }

    [Fact]
    public async Task ConfirmCancelAsync_InvokesInteraction()
    {
        await new TestScheduler().WithAsync(async s =>
        {
            var vm = new ProgressViewModel(_screen.Object, _archiveReader.Object, _indexService.Object, () => _writerSession.Object)
                .WithParameters(_importParams);

            var handlerCalled = false;

            vm.ConfirmCancel.RegisterHandler(ctx =>
            {
                handlerCalled = true;
                ctx.SetOutput(true);
            });

            var result = await vm.ConfirmCancelAsync();

            Assert.True(handlerCalled);
            Assert.True(result);
        });
    }
}
