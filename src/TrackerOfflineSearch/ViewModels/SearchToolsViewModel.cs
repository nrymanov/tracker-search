using System;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class SearchToolsViewModel : ViewModelBase<SearchToolsViewModel>
{
    public SearchToolsViewModel(IPostRepository repository, IEventAggregator eventAggregator, ILogger<SearchToolsViewModel> logger) : base(eventAggregator, logger)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));

        this.EventAggregator.GetEvent<ReporitoryChangedEvent>().Subscribe(UpdateReporitoryInfo, ThreadOption.UIThread);

        this.UpdateReporitoryInfo();
    }

    public DateTime CreationDate
    {
        get => _creationDate;
        private set => this.RaiseAndSetIfChanged(ref _creationDate, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        private set => this.RaiseAndSetIfChanged(ref _totalItems, value);
    }

    #region Private fields & methods

    private void UpdateReporitoryInfo()
    {
        this.TotalItems = this._repository.TotalItems;
    }

    private readonly IPostRepository _repository;
    private int _totalItems;
    private DateTime _creationDate = DateTime.Now;

    #endregion
}

