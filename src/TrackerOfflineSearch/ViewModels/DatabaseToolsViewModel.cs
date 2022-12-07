using System;
using System.Reactive;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;
using ReactiveUI;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.UpdateWizard.Views;

namespace TrackerOfflineSearch.ViewModels;

public class DatabaseToolsViewModel : ViewModelBase<DatabaseToolsViewModel>
{
    #region Constructor

    public DatabaseToolsViewModel(
        IPostRepository repository,
        IDialogService dialogService,
        IEventAggregator eventAggregator, 
        ILogger<DatabaseToolsViewModel> logger
        ) : base(eventAggregator, logger)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.EventAggregator.GetEvent<ReporitoryChangedEvent>().Subscribe(this.UpdateReporitoryInfo, ThreadOption.UIThread);

        this.ImportCommand = ReactiveCommand.Create(this.StartImport);

        this.UpdateReporitoryInfo();
    }

    #endregion

    #region Public properties & methods

    public DateTime CreationDate
    {
        get => this.creationDate;
        private set => this.RaiseAndSetIfChanged(ref this.creationDate, value);
    }

    public int TotalItems
    {
        get => this.totalItems;
        private set => this.RaiseAndSetIfChanged(ref this.totalItems, value);
    }

    public ReactiveCommand<Unit, Unit> ImportCommand { get; }

    #endregion

    #region Private fields & methods

    private void UpdateReporitoryInfo() => this.TotalItems = this.repository.TotalItems;

    private void StartImport() => this.dialogService.ShowDialog(nameof(RepositoryWizardView), _ => this.EventAggregator.GetEvent<ReporitoryChangedEvent>().Publish());

    private readonly IPostRepository repository;
    private readonly IDialogService dialogService;
    private int totalItems;
    private DateTime creationDate = DateTime.Now;

    #endregion
}
