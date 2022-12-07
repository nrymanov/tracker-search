using System;
using ReactiveUI;

namespace TrackerOfflineSearch.UpdateWizard.ViewModels;

public abstract class WizardPageViewModel : ReactiveObject
{
    public virtual void Activate(RepositoryWizardViewModel wizardViewModel)
    {
        this.wizard = wizardViewModel ?? throw new ArgumentNullException(nameof(wizardViewModel));
    }

    public virtual void Deactivate(RepositoryWizardViewModel wizardViewModel)
    {
        System.Diagnostics.Debug.Assert(this.wizard == wizardViewModel);

        this.wizard = null;
    }

    public virtual bool CanCancelPage()
    {
        System.Diagnostics.Debug.Assert(this.Wizard is not null);

        return true;
    }

    public string Title
    {
        get => this.title;
        protected set => this.RaiseAndSetIfChanged(ref this.title, value);
    }

    protected RepositoryWizardViewModel Wizard => this.wizard;

    private string title;
    private RepositoryWizardViewModel wizard;
}
