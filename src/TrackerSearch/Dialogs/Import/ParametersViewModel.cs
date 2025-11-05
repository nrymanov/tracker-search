using TrackerOfflineSearch.Core.Interfaces;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ParametersViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ParametersViewModel(IScreen screen)
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));

        SelectArchiveCommand = ReactiveCommand.CreateFromTask(SelectArchiveAsync);

        var canGoNext = this.WhenAnyValue(x => x.ArchivePath)
            .Select(path => !string.IsNullOrEmpty(path));

        GoNextCommand = ReactiveCommand.Create(
            () => new ImportParameters(ArchivePath, SimpleIndex, _optimization),
            canGoNext
        );

        CancelCommand = ReactiveCommand.Create(() => { });

        _indexTypeTipProperty = this.WhenAnyValue(x => x.SimpleIndex)
            .Select(GetIndexTypeDescription)
            .ToProperty(this, x => x.IndexTypeTip);

        _indexOptimizationProperty = this.WhenAnyValue(x => x.Optimization)
            .Select(x => GetOptimizationStrategyDescription((IndexOptimizationStrategy)x))
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

    public ReactiveCommand<Unit, Unit> SelectArchiveCommand { get; }

    public ReactiveCommand<Unit, ImportParameters> GoNextCommand { get; }

    // Interaction

    public Interaction<Unit, string> SelectArchive { get; } = new();

    #endregion

    #region Private

    private async Task SelectArchiveAsync()
    {
        var file = await SelectArchive.Handle(Unit.Default);
        if (string.IsNullOrEmpty(file))
        {
            return;
        }

        ArchivePath = file;
    }

    private static string GetIndexTypeDescription(bool isSimpleIndex) =>
        isSimpleIndex
            ? "Простой индекс: поиск по тексту постов недоступен, но поиск по заголовкам работает. Создаётся быстро и занимает минимум места."
            : "Полный индекс: поиск по тексту постов доступен, требует больше времени и места.";

    private static string GetOptimizationStrategyDescription(IndexOptimizationStrategy optimizationStrategy) =>
        optimizationStrategy switch
        {
            IndexOptimizationStrategy.Minimum => "Быстро выполняется, индекс крупнее и фрагментирован, поиск немного медленнее.",
            IndexOptimizationStrategy.Low => "Лёгкая оптимизация: немного меньше сегментов, поиск чуть быстрее, немного места на диске.",
            IndexOptimizationStrategy.Normal => "Сбалансированно: меньше сегментов, умеренный размер, поиск быстрее, умеренное место на диске.",
            IndexOptimizationStrategy.High => "Глубокая оптимизация: меньше файлов и размер меньше, поиск быстрее, требуется больше времени и места на диске.",
            IndexOptimizationStrategy.Maximum => "Максимум: минимальный размер и файлов, поиск самый быстрый, но оптимизация долгая и требует много временного места.",
            _ => throw new ArgumentOutOfRangeException(nameof(optimizationStrategy)),
        };

    private readonly ObservableAsPropertyHelper<string> _indexTypeTipProperty;
    private readonly ObservableAsPropertyHelper<string> _indexOptimizationProperty;

    private string _archivePath = "";
    private bool _simpleIndex;
    private IndexOptimizationStrategy _optimization = IndexOptimizationStrategy.Normal;

    #endregion
}
