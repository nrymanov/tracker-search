using TrackerOfflineSearch.ViewModels;

namespace TrackerOfflineSearch.Dialogs.Import;

public class ErrorViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ErrorViewModel(IScreen screen)
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));
        CancelCommand = ReactiveCommand.Create(() => { });
        CloseCommand = ReactiveCommand.Create(() => false);
    }

    #region IRoutableViewModel

    public string? UrlPathSegment => "import-error";

    public IScreen HostScreen { get; }

    #endregion

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    // Ask confirmation
    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

    #region Public

    public ReactiveCommand<Unit, bool> CloseCommand { get; }

    public ErrorViewModel WithParameters(ImportFailedResult importResult)
    {
        ErrorMessage = importResult.Error.Message;

        return this;
    }

    public string ErrorMessage { get; private set; } = "";

    #endregion

    #region Private

    #endregion
}
