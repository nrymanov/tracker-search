using System;
using System.Threading.Tasks;
using ReactiveUI;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.UpdateWizard.ViewModels;

public class ImportProgressPageViewModel : WizardPageViewModel
{
    #region Constructor

    public ImportProgressPageViewModel(Func<IImportManager> importManagerFactory)
    {
        this.importManagerFactory = importManagerFactory ?? throw new ArgumentNullException(nameof(importManagerFactory));

        this.Title = "Import in progress...";
    }

    #endregion

    #region WizardPageViewModel overrides

    public override async void Activate(RepositoryWizardViewModel wizardViewModel)
    {
        base.Activate(wizardViewModel);

        this.Wizard.ShowGoNext = false;

        if (await this.ImportAsync(this.Wizard.ArchivePath, this.Wizard.Optimize))
            this.Wizard.GoNext();
        else
        {
            this.Wizard.Close();
        }
    }

    public override void Deactivate(RepositoryWizardViewModel wizardViewModel)
    {
        this.Wizard.ImportTotal = this.ImportTotal;

        base.Deactivate(wizardViewModel);
    }

    public override bool CanCancelPage()
    {
        var im = this.importManager;
        if (im is null)
            return true;

        if (!this.importCanceling)
        {
            im.Cancel();
            if (this.Wizard is not null)
                this.Wizard.CanCancel = false;
            this.importCanceling = true;
        }

        return false;
    }

    #endregion

    #region Public properties

    public int ImportTotal
    {
        get => this.importTotal;
        set => this.RaiseAndSetIfChanged(ref this.importTotal, value);
    }

    #endregion

    #region Private fields & methods

    private async Task<bool> ImportAsync(string archivePath, bool optimize)
    {
        this.ImportTotal = 0;

        this.importManager = this.importManagerFactory();
        try
        {
            using var s1 = this.importManager
                .WhenAnyValue(im => im.ImportCount)
                .Subscribe(total => this.ImportTotal = total);

            await this.importManager.ImportAsync(archivePath);

            if (optimize)
                await this.importManager.OptimizeAsync();

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            this.importManager = null;
        }
    }

    private readonly Func<IImportManager> importManagerFactory;
    private IImportManager importManager;
    private bool importCanceling;
    private int importTotal;

    #endregion
}
