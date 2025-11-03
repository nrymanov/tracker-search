using TrackerOfflineSearch.Core.Interfaces;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ResultViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ResultViewModel(IScreen screen)
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));
        CancelCommand = ReactiveCommand.Create(() => { });
    }

    #region IRoutableViewModel

    public string? UrlPathSegment => "import-result";

    public IScreen HostScreen { get; }

    #endregion

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    // Ask confirmation
    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

    #region Public

    public ResultViewModel WithParameters(ImportResult importResult)
    {
        ArchivePath = importResult.Parameters.ArchivePath;
        SimpleIndex = Map(importResult.Parameters.SimpleIndex);
        IndexOptimization = Map(importResult.Parameters.IndexOptimization);

        TotalDocuments = importResult.TotalDocuments;
        ElapsedTime = TimeSpan.FromSeconds((long)importResult.Elapsed.TotalSeconds);

        return this;
    }

    public string ArchivePath { get; private set; } = "";

    public string SimpleIndex { get; private set; } = "";

    public string IndexOptimization { get; private set; } = "";

    public int TotalDocuments { get; private set; }

    public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;

    #endregion

    #region Private

    private static string Map(bool isSimpleIndex) =>
        isSimpleIndex ? "Да" : "Нет";

    private static string Map(IndexOptimizationStrategy optimizationStrategy) =>
        optimizationStrategy switch
        {
            IndexOptimizationStrategy.Minimum => "Минимальная",
            IndexOptimizationStrategy.Low => "Слабая",
            IndexOptimizationStrategy.Normal => "Обычная",
            IndexOptimizationStrategy.High => "Высокая",
            IndexOptimizationStrategy.Maximum => "Максимальная",

            _ => throw new ArgumentOutOfRangeException(nameof(optimizationStrategy)),
        };

    #endregion
}
