using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Services.Dialogs;
using ReactiveUI;

namespace TrackerOfflineSearch.UpdateWizard.ViewModels;

public class RepositoryWizardViewModel : ReactiveObject, IDialogAware
{
    #region Constructor

    public RepositoryWizardViewModel(
        ImportParametersPageViewModel page1,
        ImportProgressPageViewModel page2,
        ImportResultPageViewModel page3
        )
    {
        // Go Next
        this.showGoNext = true;
        var canGoNext = this.WhenAnyValue(w => w.CanGoNext);
        this.GoNextCommand = ReactiveCommand.Create(this.GoNext, canGoNext);

        // Cancel
        this.cancelTitle = "Cancel";
        var canCancel = this.WhenAnyValue(w => w.CanCancel);
        this.CancelCommand = ReactiveCommand.Create(this.Close, canCancel);

        // Current Page
        this.pages = new() { page1, page2, page3 };
        this.Current = this.pages.First();
    }

    #endregion

    #region IDialogAware implementation

    public string Title { get; } = "Import Wizard";

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public event Action<IDialogResult> RequestClose;

    public bool CanCloseDialog()
    {
        System.Diagnostics.Debug.Assert(this.Current is not null);

        return this.Current.CanCancelPage();
    }

    public void OnDialogClosed()
    {
    }

    #endregion

    #region Wizard state field

    //
    // These fields should not send change notifications.
    //

    public string ArchivePath
    {
        get;
        set;
    }

    public bool Optimize
    {
        get;
        set;
    }

    public int ImportTotal
    {
        get;
        set;
    }

    #endregion

    #region Public properties & methods

    public bool ShowGoNext
    {
        get => this.showGoNext;
        set => this.RaiseAndSetIfChanged(ref this.showGoNext, value);
    }

    public bool CanGoNext
    {
        get => this.canGoNext;
        set => this.RaiseAndSetIfChanged(ref this.canGoNext, value);
    }

    public ICommand GoNextCommand { get; }

    public string CancelTitle
    {
        get => this.cancelTitle;
        set => this.RaiseAndSetIfChanged(ref this.cancelTitle, value);
    }

    public bool CanCancel
    {
        get => this.canCancel;
        set => this.RaiseAndSetIfChanged(ref this.canCancel, value);
    }

    public ICommand CancelCommand { get; }

    public WizardPageViewModel Current
    {
        get => this.current;
        private set
        {
            if (this.current == value)
                return;

            this.current?.Deactivate(this);

            this.current = value;

            this.current?.Activate(this);

            //if (this.IsLastPage)
            //{
            //    this.ShowGoNext = false;
            //    this.CancelTitle = "Close";
            //}

            this.RaisePropertyChanged();
        }
    }

    #endregion

    #region Internal fields & methods

    internal void GoNext()
    {
        System.Diagnostics.Debug.Assert(this.pages.IndexOf(this.Current) < this.pages.Count - 1);
        this.Current = this.pages[this.pages.IndexOf(this.Current) + 1];
    }

    internal void Close()
    {
        this.RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
    }

    #endregion

    #region Private fields & methods

    private bool canGoNext = true;
    private bool canCancel = true;
    private string cancelTitle;
    private bool showGoNext;

    private readonly List<WizardPageViewModel> pages;
    private WizardPageViewModel current;

    #endregion
}
