using ReactiveUI;

namespace TrackerOfflineSearch.UpdateWizard.ViewModels;

public class ImportResultPageViewModel : WizardPageViewModel
{
    #region Constructor

    public ImportResultPageViewModel()
    {
        this.Title = "Import completed";
    }

    #endregion

    #region WizardPageViewModel overrides

    public override void Activate(RepositoryWizardViewModel wizardViewModel)
    {
        base.Activate(wizardViewModel);

        this.ImportTotal = this.Wizard.ImportTotal;

        this.Wizard.ShowGoNext = false;
        this.Wizard.CancelTitle = "Close";
    }

    #endregion

    #region Public properties

    public int ImportTotal
    {
        get => this.importTotal;
        private set => this.RaiseAndSetIfChanged(ref this.importTotal, value);
    }

    #endregion

    #region Private fields & methods

    private int importTotal;

    #endregion
}
