using TrackerOfflineSearch.Services.Models;
using TrackerOfflineSearch.ViewModels;

namespace TrackerOfflineSearch.Dialogs.Import;

public class ResultViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ResultViewModel(IScreen screen)
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));
        CancelCommand = ReactiveCommand.Create(() => { });
        CloseCommand = ReactiveCommand.Create(() => true);
    }

    #region IRoutableViewModel

    public string? UrlPathSegment => "import-result";

    public IScreen HostScreen { get; }

    #endregion

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

    #region Public

    public ReactiveCommand<Unit, bool> CloseCommand { get; }

    public ResultViewModel WithParameters(ImportCompletedResult importResult)
    {
        ArchivePath = importResult.Parameters.ArchivePath;
        SimpleIndex = ToDisplayString(importResult.Parameters.SimpleIndex);
        IndexOptimization = ToDisplayString(importResult.Parameters.IndexOptimization);

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

    private static string ToDisplayString(bool isSimpleIndex) =>
        isSimpleIndex ? "Да" : "Нет";

    private static string ToDisplayString(IndexOptimizationStrategy optimizationStrategy) =>
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
