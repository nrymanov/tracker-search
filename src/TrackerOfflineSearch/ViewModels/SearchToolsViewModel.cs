using System;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class SearchToolsViewModel : ViewModelBase<SearchToolsViewModel>
{
    #region Constructor

    public SearchToolsViewModel(IPostRepository repository, IEventAggregator eventAggregator, ILogger<SearchToolsViewModel> logger) : base(eventAggregator, logger)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

        this.EventAggregator.GetEvent<ReporitoryChangedEvent>().Subscribe(this.UpdateReporitoryInfo, ThreadOption.UIThread);

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

    #endregion

    #region Private fields & methods

    private void UpdateReporitoryInfo() => this.TotalItems = this.repository.TotalItems;

    private readonly IPostRepository repository;
    private int totalItems;
    private DateTime creationDate = DateTime.Now;

    #endregion
}
