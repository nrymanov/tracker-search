using Moq;
using ReactiveUI;
using TrackerOfflineSearch.Dialogs.Import;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.UnitTests.Helpers;

namespace TrackerOfflineSearch.UnitTests.Dialogs.Import;

public class ImportWizardViewLocatorTests
{
    private readonly ImportWizardViewLocator _locator = new();

    [Fact]
    public void Resolve_ParametersViewModel_Returns_ParametersView()
    {
        ReactiveUIApp.WithFakeActivation(() =>
        {
            // Arrange
            var vm = new ParametersViewModel(Mock.Of<IScreen>());

            // Act
            var view = _locator.ResolveView(vm);

            // Assert
            Assert.NotNull(view);
            Assert.IsType<ParametersView>(view);
        });
    }

    [Fact]
    public void Resolve_ProgressViewModel_Returns_ProgressView()
    {
        ReactiveUIApp.WithFakeActivation(() =>
        {
            var vm = new ProgressViewModel(
                Mock.Of<IScreen>(),
                Mock.Of<IArchiveReader>(),
                Mock.Of<IIndexService>(),
                () => new Mock<IIndexWriterSession>().Object
            );

            var view = _locator.ResolveView(vm);

            Assert.NotNull(view);
            Assert.IsType<ProgressView>(view);
        });
    }

    [Fact]
    public void Resolve_ResultViewModel_Returns_ResultView()
    {
        ReactiveUIApp.WithFakeActivation(() =>
        {
            var vm = new ResultViewModel(Mock.Of<IScreen>());

            var view = _locator.ResolveView(vm);

            Assert.NotNull(view);
            Assert.IsType<ResultView>(view);
        });
    }

    [Fact]
    public void Resolve_ErrorViewModel_Returns_ErrorView()
    {
        ReactiveUIApp.WithFakeActivation(() =>
        {
            var vm = new ErrorViewModel(Mock.Of<IScreen>());

            var view = _locator.ResolveView(vm);

            Assert.NotNull(view);
            Assert.IsType<ErrorView>(view);
        });
    }

    [Fact]
    public void Resolve_UnknownViewModel_ThrowsArgumentOutOfRange()
    {
        ReactiveUIApp.WithFakeActivation(() =>
        {
            var vm = new object();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _locator.ResolveView(vm);
            });
        });
    }

    [Fact]
    public void Resolve_IgnoresContractParameter()
    {
        ReactiveUIApp.WithFakeActivation(() =>
        {
            var vm = new ParametersViewModel(Mock.Of<IScreen>());

            var view = _locator.ResolveView(vm, contract: "ignored");

            Assert.NotNull(view);
            Assert.IsType<ParametersView>(view);
        });
    }
}
