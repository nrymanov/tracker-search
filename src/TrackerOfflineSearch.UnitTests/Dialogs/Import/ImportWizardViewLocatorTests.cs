using Moq;
using ReactiveUI;
using TrackerOfflineSearch.Dialogs.Import;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.UnitTests.Helpers;

namespace TrackerOfflineSearch.UnitTests.Dialogs.Import;

public class ImportWizardViewLocatorTests : IClassFixture<DisableReactiveUIActivation>
{
    private readonly Mock<IScreen> _screenMock = new();
    private readonly Mock<IArchiveReader> _archiveReaderMock = new();
    private readonly Mock<IIndexService> _indexServiceMock = new();
    private readonly ImportWizardViewLocator _locator = new();

    public ImportWizardViewLocatorTests(DisableReactiveUIActivation _)
    {
    }

    [Fact]
    public void Resolve_ParametersViewModel_Returns_ParametersView()
    {
        // Arrange
        var vm = new ParametersViewModel(_screenMock.Object);

        // Act
        var view = _locator.ResolveView(vm);

        // Assert
        Assert.NotNull(view);
        Assert.IsType<ParametersView>(view);
    }

    [Fact]
    public void Resolve_ProgressViewModel_Returns_ProgressView()
    {
        var f = () => new Mock<IIndexWriterSession>().Object;

        var vm = new ProgressViewModel(_screenMock.Object, _archiveReaderMock.Object, _indexServiceMock.Object, f);

        var view = _locator.ResolveView(vm);

        Assert.NotNull(view);
        Assert.IsType<ProgressView>(view);
    }

    [Fact]
    public void Resolve_ResultViewModel_Returns_ResultView()
    {
        var vm = new ResultViewModel(_screenMock.Object);

        var view = _locator.ResolveView(vm);

        Assert.NotNull(view);
        Assert.IsType<ResultView>(view);
    }

    [Fact]
    public void Resolve_ErrorViewModel_Returns_ErrorView()
    {
        var vm = new ErrorViewModel(_screenMock.Object);

        var view = _locator.ResolveView(vm);

        Assert.NotNull(view);
        Assert.IsType<ErrorView>(view);
    }

    [Fact]
    public void Resolve_UnknownViewModel_ThrowsArgumentOutOfRange()
    {
        var vm = new object();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _locator.ResolveView(vm);
        });
    }

    [Fact]
    public void Resolve_IgnoresContractParameter()
    {
        var vm = new ParametersViewModel(_screenMock.Object);

        var view = _locator.ResolveView(vm, contract: "ignored");

        Assert.NotNull(view);
        Assert.IsType<ParametersView>(view);
    }
}
