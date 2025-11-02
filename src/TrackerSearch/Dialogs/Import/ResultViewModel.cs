using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ResultViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ResultViewModel(IScreen screen)
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));
        CancelCommand = ReactiveCommand.Create(() => { });
    }

    public string? UrlPathSegment => "import-result";

    public IScreen HostScreen { get; }

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    // Ask confirmation
    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

}
