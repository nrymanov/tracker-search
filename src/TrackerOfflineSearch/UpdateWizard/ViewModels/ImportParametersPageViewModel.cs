using System;
using System.Reactive;
using Microsoft.Win32;
using ReactiveUI;

namespace TrackerOfflineSearch.UpdateWizard.ViewModels;

public class ImportParametersPageViewModel : WizardPageViewModel
{
    #region Constructor

    public ImportParametersPageViewModel(Func<OpenFileDialog> openFileDialogFactory)
    {
        this.openFileDialogFactory = openFileDialogFactory ?? throw new ArgumentNullException(nameof(openFileDialogFactory));

        this.Title = "Welcom to the Import Wizard!";

        this.SelectArchiveCommand = ReactiveCommand.Create(this.SelectArchive);

        this.WhenAnyValue(vm => vm.ArchivePath)
            .Subscribe(path =>
                {
                    if (this.Wizard is null)
                        return;

                    this.Wizard.CanGoNext = !string.IsNullOrEmpty(path);
                }
            );
    }

    #endregion

    #region WizardPageViewModel overrides

    public override void Activate(RepositoryWizardViewModel wizardViewModel)
    {
        base.Activate(wizardViewModel);

        this.Wizard.CanGoNext = !string.IsNullOrEmpty(this.ArchivePath);
    }

    public override void Deactivate(RepositoryWizardViewModel wizardViewModel)
    {
        this.Wizard.ArchivePath = this.ArchivePath;
        this.Wizard.Optimize = this.Optimize;

        base.Deactivate(wizardViewModel);
    }

    #endregion

    #region Public properties

    public string ArchivePath
    {
        get => this.archivePath;
        set => this.RaiseAndSetIfChanged(ref this.archivePath, value);
    }

    public bool Optimize
    {
        get => this.optimize;
        set => this.RaiseAndSetIfChanged(ref this.optimize, value);
    }

    public ReactiveCommand<Unit, Unit> SelectArchiveCommand { get; }

    #endregion

    #region Private fields & methods

    private void SelectArchive()
    {
        var openFileDialog = this.openFileDialogFactory();
        openFileDialog.Filter = "Archives (*.xml.xz)|*.xml.xz|All files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true)
            this.ArchivePath = openFileDialog.FileName;
    }

    private string archivePath;
    private bool optimize;
    private readonly Func<OpenFileDialog> openFileDialogFactory;

    #endregion
}
