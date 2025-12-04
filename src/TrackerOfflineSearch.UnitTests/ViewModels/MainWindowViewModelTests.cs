using System.Diagnostics;
using System.Reactive;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Models;
using TrackerOfflineSearch.UnitTests.Helpers;
using TrackerOfflineSearch.ViewModels;
using Xunit.Abstractions;

namespace TrackerOfflineSearch.UnitTests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly ITestOutputHelper _output;

    private readonly Mock<ILogger<MainWindowViewModel>> _logger = new();
    private readonly Mock<IIndexService> _indexService = new();
    private readonly Mock<IBBTextConverter> _bbConverter = new();
    //private readonly IBackgroundRunner _runner;
    //private readonly TestScheduler _scheduler = new();
    //private readonly MainWindowViewModel _vm;

    public MainWindowViewModelTests(ITestOutputHelper output)
    {
        _output = output;

        //_runner = new TestSchedulerBackgroundRunner(_scheduler);
        //_vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, _runner);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        new TestScheduler().With(s =>
        {
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            // Assert
            Assert.NotNull(vm.Forums);
            Assert.Null(vm.SelectedForum);
            Assert.Equal("", vm.ForumFilterText);

            Assert.NotNull(vm.Posts);
            Assert.Null(vm.SelectedPost);
            Assert.Null(vm.SelectedPostInfo);
            Assert.Equal("", vm.PostFilterText);

            Assert.NotNull(vm.Import);
            Assert.NotNull(vm.ImportCommand);

            Assert.NotNull(vm.About);
            Assert.NotNull(vm.AboutCommand);
        });
    }

    [Fact]
    public void WhenActivated_ForumsAndPostsAreLoaded()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            _indexService.Setup(x => x.GetForums()).Returns([new Forum("Root")]);

            _indexService
                .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
                .Returns(new SearchResult([
                    new Post { Id = 1, Title = "Post1" },
                    new Post { Id = 2, Title = "Post2" },
                    new Post { Id = 3, Title = "Post3" },
                    new Post { Id = 4, Title = "Post4" },
                ], 10));

            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            // Before Activate Assert
            Assert.Empty(vm.Forums);
            Assert.Empty(vm.Posts);

            // Act
            vm.Activator.Activate();

            // After Activate Assert
            Assert.Empty(vm.Forums);
            Assert.Empty(vm.Posts);

            // Move timer
            s.AdvanceBy(2);

            // Final Assert
            Assert.Single(vm.Forums);
            Assert.Equal(4, vm.Posts.Count);

            _indexService.Verify(x => x.GetForums(), Times.Once);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
        });
    }

    [Fact]
    public void ImportCommand_Should_Invoke_Interaction()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            var interactionCalled = false;
            vm.Import.RegisterHandler(ctx =>
            {
                interactionCalled = true;
                ctx.SetOutput(true);
            });

            // Act
            vm.ImportCommand.Execute().Subscribe();

            // Assert
            Assert.True(interactionCalled);
        });
    }

    [Fact]
    public void WhenImportIsSuccessful_ThenPostsAndForumsAreReloaded()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            _indexService.Setup(x => x.GetForums()).Returns([new Forum("Root")]);

            _indexService
                .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
                .Returns(new SearchResult([
                    new Post { Id = 1, Title = "Post1" },
                    new Post { Id = 2, Title = "Post2" },
                    new Post { Id = 3, Title = "Post3" },
                    new Post { Id = 4, Title = "Post4" },
                ], 10));

            bool importInteractionHandled = false;
            vm.Import.RegisterHandler(ctx =>
            {
                _output.WriteLine("Import Interaction Handled");
                importInteractionHandled = true;
                ctx.SetOutput(true);
            });

            // Act
            _output.WriteLine("1. " + string.Join('\n', _indexService.Invocations.Select(i => i.ToString())));

            vm.Activator.Activate();

            _output.WriteLine("2. " + string.Join('\n', _indexService.Invocations.Select(i => i.ToString())));

            s.AdvanceBy(2);

            _output.WriteLine("3. " + string.Join('\n', _indexService.Invocations.Select(i => i.ToString())));

            // Assert Before Command
            _indexService.Verify(x => x.GetForums(), Times.Once);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

            vm.ImportCommand.Execute().Subscribe();

            _output.WriteLine("4. " + string.Join('\n', _indexService.Invocations.Select(i => i.ToString())));
            s.AdvanceBy(30);
            _output.WriteLine("5. " + string.Join('\n', _indexService.Invocations.Select(i => i.ToString())));

            s.Start();
            _output.WriteLine("6. " + string.Join('\n', _indexService.Invocations.Select(i => i.ToString())));

            // Assert After Command
            Assert.True(importInteractionHandled);
            _indexService.Verify(x => x.GetForums(), Times.Exactly(2));
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Exactly(2));
            _indexService.VerifyNoOtherCalls();
        });
    }

    [Fact]
    public void WhenImportFails_ThenNoReloadIsTriggered()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            _indexService.Setup(x => x.GetForums()).Returns([new Forum("Root")]);

            _indexService
                .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
                .Returns(new SearchResult([
                    new Post { Id = 1, Title = "Post1" },
                new Post { Id = 2, Title = "Post2" },
                new Post { Id = 3, Title = "Post3" },
                new Post { Id = 4, Title = "Post4" },
                ], 10));

            vm.Import.RegisterHandler(ctx =>
            {
                ctx.SetOutput(false);
            });

            // Act
            vm.Activator.Activate();

            s.AdvanceBy(2);

            // Assert Before Command
            _indexService.Verify(x => x.GetForums(), Times.Once);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

            _indexService.Invocations.Clear();
            _indexService.VerifyNoOtherCalls();

            vm.ImportCommand.Execute().Subscribe();

            s.AdvanceBy(3);

            // Assert After Command
            _indexService.VerifyNoOtherCalls();
        });
    }

    [Fact]
    public void AboutCommand_Should_Invoke_AboutInteraction()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            var called = false;
            vm.About.RegisterHandler(ctx =>
            {
                called = true;
                ctx.SetOutput(Unit.Default);
            });

            vm.AboutCommand.Execute().Subscribe();

            Assert.True(called);
        });
    }

    [Fact]
    public void WhenForumFilterTextChanges_ThenForumsAreFiltered()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            _indexService
                .Setup(x => x.GetForums())
                .Returns([new Forum("Programming"), new Forum("Music")]);

            _indexService
                .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
                .Returns(new SearchResult([new Post { Id = 1 }], 10));

            vm.Activator.Activate();

            s.AdvanceBy(2);

            Assert.Equal(2, vm.Forums.Count);

            // Act
            vm.ForumFilterText = "Prog";

            s.AdvanceBy(TimeSpan.FromMilliseconds(600).Ticks);

            // Assert
            Assert.Single(vm.Forums);
            Assert.Equal("Programming", vm.Forums[0].Name);
            _indexService.Verify(x => x.GetForums(), Times.Once);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
            _indexService.VerifyNoOtherCalls();
        });
    }

    [Fact]
    public void WhenPostIsSelected_ThenPostInfoIsUpdated()
    {
        new TestScheduler().With(s =>
        {
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            var post = new Post
            {
                Id = 10,
                Content = "[b]hello[/b]",
                Title = "Title",
            };

            _bbConverter.Setup(x => x.Convert("[b]hello[/b]")).Returns("<b>hello</b>");

            vm.Activator.Activate();

            vm.SelectedPost = post;
            s.AdvanceBy(TimeSpan.FromMilliseconds(600).Ticks);
            //_scheduler.Start();

            Assert.NotNull(vm.SelectedPostInfo);
            Assert.Equal(post.Title, vm.SelectedPostInfo.Title);
            Assert.Equal("<b>hello</b>", vm.SelectedPostInfo.Content);
        });
    }

    [Fact]
    public void WhenSelectedPostIsCleared_ThenPostInfoIsCleared()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            var post = new Post { Id = 1, Title = "Test", Content = "Content" };
            vm.SelectedPost = post;
            s.AdvanceBy(1);

            // Act
            vm.SelectedPost = null;
            s.AdvanceBy(1);

            // Assert
            Assert.Null(vm.SelectedPostInfo);
        });
    }

    [Fact]
    public void WhenForumIsSelected_ThenSearchIsExecutedWithDelay()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            var forum = new Forum("Root");
            var node = new DynamicData.Node<Forum, string>(forum, forum.Id);

            var forumSortComparer = SortExpressionComparer<ForumViewModel>.Ascending(f => f.Order).ThenByAscending(x => x.Name);

            _indexService.Setup(x => x.GetForums()).Returns([forum]);

            _indexService
                .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
                .Returns(new SearchResult([
                    new Post { Id = 1, Title = "Post1" },
                    new Post { Id = 2, Title = "Post2" },
                    new Post { Id = 3, Title = "Post3" },
                    new Post { Id = 4, Title = "Post4" },
                ], 10));

            vm.Activator.Activate();

            s.AdvanceBy(2);

            // Assert Before Command
            _indexService.Verify(x => x.GetForums(), Times.Once);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

            _indexService.Invocations.Clear();
            _indexService.VerifyNoOtherCalls();

            // Act
            vm.SelectedForum = new ForumViewModel(node, forumSortComparer);

            // Small delay
            s.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);
            _indexService.VerifyNoOtherCalls();

            s.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

            // Assert After Command
            _indexService.Verify(x => x.GetForums(), Times.Never);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
            _indexService.VerifyNoOtherCalls();
        });
    }

    [Fact]
    public void WhenPostFilterTextChanges_ThenSearchIsExecutedWithDelay()
    {
        new TestScheduler().With(s =>
        {
            // Arrange
            var runner = new TestSchedulerBackgroundRunner(s);
            var vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, runner);

            _indexService.Setup(x => x.GetForums()).Returns([new Forum("Root")]);

            _indexService
                .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
                .Returns(new SearchResult([
                    new Post { Id = 1, Title = "Post1" },
                    new Post { Id = 2, Title = "Post2" },
                    new Post { Id = 3, Title = "Post3" },
                    new Post { Id = 4, Title = "Post4" },
                ], 10));

            vm.Activator.Activate();

            s.AdvanceBy(2);

            // Assert Before Command
            _indexService.Verify(x => x.GetForums(), Times.Once);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

            _indexService.Invocations.Clear();
            _indexService.VerifyNoOtherCalls();

            // Act
            vm.PostFilterText = "post";

            // Small delay
            s.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);
            _indexService.VerifyNoOtherCalls();

            s.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

            // Assert After Command
            _indexService.Verify(x => x.GetForums(), Times.Never);
            _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
            _indexService.VerifyNoOtherCalls();
        });
    }
}
