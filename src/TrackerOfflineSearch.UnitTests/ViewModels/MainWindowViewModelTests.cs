using System.Reactive;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Models;
using TrackerOfflineSearch.UnitTests.Helpers;
using TrackerOfflineSearch.ViewModels;

namespace TrackerOfflineSearch.UnitTests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<ILogger<MainWindowViewModel>> _logger = new();
    private readonly Mock<IIndexService> _indexService = new();
    private readonly Mock<IBBTextConverter> _bbConverter = new();
    private readonly IBackgroundRunner _runner;
    private readonly TestScheduler _scheduler = new();
    private readonly MainWindowViewModel _vm;

    public MainWindowViewModelTests()
    {
        RxApp.MainThreadScheduler = _scheduler;
        RxApp.TaskpoolScheduler = _scheduler;

        _runner = new TestSchedulerBackgroundRunner(_scheduler);

        _vm = new MainWindowViewModel(_logger.Object, _indexService.Object, _bbConverter.Object, _runner);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        Assert.NotNull(_vm.Forums);
        Assert.Null(_vm.SelectedForum);
        Assert.Equal("", _vm.ForumFilterText);

        Assert.NotNull(_vm.Posts);
        Assert.Null(_vm.SelectedPost);
        Assert.Null(_vm.SelectedPostInfo);
        Assert.Equal("", _vm.PostFilterText);

        Assert.NotNull(_vm.Import);
        Assert.NotNull(_vm.ImportCommand);

        Assert.NotNull(_vm.About);
        Assert.NotNull(_vm.AboutCommand);
    }

    [Fact]
    public void WhenActivated_ForumsAndPostsAreLoaded()
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

        // Before Activate Assert
        Assert.Empty(_vm.Forums);
        Assert.Empty(_vm.Posts);

        // Act
        _vm.Activator.Activate();

        // After Activate Assert
        Assert.Empty(_vm.Forums);
        Assert.Empty(_vm.Posts);

        // Move timer
        _scheduler.AdvanceBy(2);

        // Final Assert
        Assert.Single(_vm.Forums);
        Assert.Equal(4, _vm.Posts.Count);

        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void ImportCommand_Should_Invoke_Interaction()
    {
        // Arrange
        var interactionCalled = false;
        _vm.Import.RegisterHandler(ctx =>
        {
            interactionCalled = true;
            ctx.SetOutput(true);
        });

        // Act
        _vm.ImportCommand.Execute().Subscribe();

        // Assert
        Assert.True(interactionCalled);
    }

    [Fact]
    public void WhenImportIsSuccessful_ThenPostsAndForumsAreReloaded()
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

        _vm.Import.RegisterHandler(ctx =>
        {
            ctx.SetOutput(true);
        });

        // Act
        _vm.Activator.Activate();

        _scheduler.AdvanceBy(2);

        // Assert Before Command
        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

        _indexService.Invocations.Clear();
        _indexService.VerifyNoOtherCalls();

        _vm.ImportCommand.Execute().Subscribe();

        _scheduler.AdvanceBy(3);

        // Assert After Command
        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
        _indexService.VerifyNoOtherCalls();
    }
    
    [Fact]
    public void WhenImportFails_ThenNoReloadIsTriggered()
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

        _vm.Import.RegisterHandler(ctx =>
        {
            ctx.SetOutput(false);
        });

        // Act
        _vm.Activator.Activate();

        _scheduler.AdvanceBy(2);

        // Assert Before Command
        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

        _indexService.Invocations.Clear();
        _indexService.VerifyNoOtherCalls();

        _vm.ImportCommand.Execute().Subscribe();

        _scheduler.AdvanceBy(3);

        // Assert After Command
        _indexService.VerifyNoOtherCalls();
    }

    [Fact]
    public void AboutCommand_Should_Invoke_AboutInteraction()
    {
        var called = false;
        _vm.About.RegisterHandler(ctx =>
        {
            called = true;
            ctx.SetOutput(Unit.Default);
        });

        _vm.AboutCommand.Execute().Subscribe();

        Assert.True(called);
    }

    [Fact]
    public void WhenForumFilterTextChanges_ThenForumsAreFiltered()
    {
        // Arrange
        _indexService
            .Setup(x => x.GetForums())
            .Returns([new Forum("Programming"), new Forum("Music")]);

        _indexService
            .Setup(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()))
            .Returns(new SearchResult([new Post { Id = 1 }], 10));

        _vm.Activator.Activate();

        _scheduler.AdvanceBy(2);

        Assert.Equal(2, _vm.Forums.Count);

        // Act
        _vm.ForumFilterText = "Prog";

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(600).Ticks);

        // Assert
        Assert.Single(_vm.Forums);
        Assert.Equal("Programming", _vm.Forums[0].Name);
        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
        _indexService.VerifyNoOtherCalls();
    }

    [Fact]
    public void WhenPostIsSelected_ThenPostInfoIsUpdated()
    {
        var post = new Post
        {
            Id = 10,
            Content = "[b]hello[/b]",
            Title = "Title",
        };

        _bbConverter.Setup(x => x.Convert("[b]hello[/b]")).Returns("<b>hello</b>");

        _vm.Activator.Activate();

        _vm.SelectedPost = post;
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(600).Ticks);
        //_scheduler.Start();

        Assert.NotNull(_vm.SelectedPostInfo);
        Assert.Equal(post.Title, _vm.SelectedPostInfo.Title);
        Assert.Equal("<b>hello</b>", _vm.SelectedPostInfo.Content);
    }

    [Fact]
    public void WhenSelectedPostIsCleared_ThenPostInfoIsCleared()
    {
        // Arrange
        var post = new Post { Id = 1, Title = "Test", Content = "Content" };
        _vm.SelectedPost = post;
        _scheduler.AdvanceBy(1);

        // Act
        _vm.SelectedPost = null;
        _scheduler.AdvanceBy(1);

        // Assert
        Assert.Null(_vm.SelectedPostInfo);
    }

    [Fact]
    public void WhenForumIsSelected_ThenSearchIsExecutedWithDelay()
    {
        // Arrange
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

        _vm.Activator.Activate();

        _scheduler.AdvanceBy(2);

        // Assert Before Command
        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

        _indexService.Invocations.Clear();
        _indexService.VerifyNoOtherCalls();

        // Act
        _vm.SelectedForum = new ForumViewModel(node, forumSortComparer);

        // Small delay
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);
        _indexService.VerifyNoOtherCalls();

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

        // Assert After Command
        _indexService.Verify(x => x.GetForums(), Times.Never);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
        _indexService.VerifyNoOtherCalls();
    }

    [Fact]
    public void WhenPostFilterTextChanges_ThenSearchIsExecutedWithDelay()
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

        _vm.Activator.Activate();

        _scheduler.AdvanceBy(2);

        // Assert Before Command
        _indexService.Verify(x => x.GetForums(), Times.Once);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);

        _indexService.Invocations.Clear();
        _indexService.VerifyNoOtherCalls();

        // Act
        _vm.PostFilterText = "post";

        // Small delay
        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);
        _indexService.VerifyNoOtherCalls();

        _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(300).Ticks);

        // Assert After Command
        _indexService.Verify(x => x.GetForums(), Times.Never);
        _indexService.Verify(x => x.Search(It.IsAny<PostQuery>(), It.IsAny<int>()), Times.Once);
        _indexService.VerifyNoOtherCalls();
    }
}
