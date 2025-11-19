using System.Reactive;
using System.Reactive.Linq;
using Moq;
using ReactiveUI;
using TrackerOfflineSearch.Dialogs.Import;

namespace TrackerOfflineSearch.UnitTests.Dialogs.Import;

public class ErrorViewModelTests
{
    private readonly Mock<IScreen> _screenMock = new();
    private readonly ErrorViewModel _vm;

    public ErrorViewModelTests()
    {
        _vm = new(_screenMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(_vm.UrlPathSegment));
        Assert.Equal(_screenMock.Object, _vm.HostScreen);
        Assert.NotNull(_vm.CancelCommand);
        Assert.NotNull(_vm.CloseCommand);
        Assert.True(string.IsNullOrEmpty(_vm.ErrorMessage));
    }

    [Fact]
    public async Task ConfirmCancel_ReturnsTrue()
    {
        // Act
        var confirm = await _vm.ConfirmCancelAsync();

        // Assert
        Assert.True(confirm);
    }

    [Fact]
    public async Task CancelCommand_ShouldReturnFalse()
    {
        // Act
        var result = await _vm.CancelCommand.Execute();

        // Assert
        Assert.Equal(Unit.Default, result);
    }

    [Fact]
    public async Task CloseCommand_ShouldReturnFalse()
    {
        // Act
        var result = await _vm.CloseCommand.Execute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WithParameters_ShouldCheckArgument()
    {
        // Act
        Assert.Throws<ArgumentNullException>(() => _vm.WithParameters(null!));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "It is just for testing purpuse")]
    public void WithParameters_ShouldSetErrorMessage()
    {
        // Arrange
        var importArgs = new ImportParameters("path", false, TrackerOfflineSearch.Services.Models.IndexOptimizationStrategy.Normal);
        var err = new Exception("error message");
        var importResult = new ImportFailedResult(importArgs, err);

        // Act
        var result = _vm.WithParameters(importResult);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_vm, result);
        Assert.Equal(err.Message, result.ErrorMessage);
    }
}
