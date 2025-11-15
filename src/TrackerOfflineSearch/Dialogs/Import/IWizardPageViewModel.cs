namespace TrackerOfflineSearch.Dialogs.Import;

public interface IWizardPageViewModel : IRoutableViewModel
{
    ReactiveCommand<Unit, Unit> CancelCommand { get; }

    Task<bool> ConfirmCancelAsync();
}
