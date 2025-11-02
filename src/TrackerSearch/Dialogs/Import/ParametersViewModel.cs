using TrackerOfflineSearch.Core.Interfaces;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ParametersViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ParametersViewModel(IScreen screen)
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));

        var canGoNext = this.WhenAnyValue(x => x.ArchivePath)
            .Select(path => !string.IsNullOrEmpty(path));

        GoNextCommand = ReactiveCommand.Create(
            () => new ImportParameters(ArchivePath, SimpleIndex, _optimization),
            canGoNext
        );

        CancelCommand = ReactiveCommand.Create(() => { });

        _indexTypeTipProperty = this.WhenAnyValue(x => x.SimpleIndex)
            .Select(simple => $"Is simple index = {simple}")
            .ToProperty(this, x => x.IndexTypeTip);

        _indexOptimizationProperty = this.WhenAnyValue(x => x.Optimization)
            .Select(level => $"Optimization level = {level}")
            .ToProperty(this, x => x.IndexOptimizationTip);
    }

    #region IRoutableViewModel

    public string? UrlPathSegment => "import-parameters";

    public IScreen HostScreen { get; }

    #endregion

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

    #region Public

    public string ArchivePath
    {
        get => _archivePath;
        set => this.RaiseAndSetIfChanged(ref _archivePath, value);
    }

    public bool SimpleIndex
    {
        get => _simpleIndex;
        set => this.RaiseAndSetIfChanged(ref _simpleIndex, value);
    }

    public string IndexTypeTip => _indexTypeTipProperty.Value;

    public int Optimization
    {
        get => (int)_optimization;
        set => this.RaiseAndSetIfChanged(ref _optimization, (IndexOptimizationStrategy)value);
    }

    public string IndexOptimizationTip => _indexOptimizationProperty.Value;

    public ReactiveCommand<Unit, ImportParameters> GoNextCommand { get; }

    #endregion

    #region Private

    private readonly ObservableAsPropertyHelper<string> _indexTypeTipProperty;
    private readonly ObservableAsPropertyHelper<string> _indexOptimizationProperty;

    private string _archivePath = "";
    private bool _simpleIndex;
    private IndexOptimizationStrategy _optimization = IndexOptimizationStrategy.Normal;

    #endregion
}
