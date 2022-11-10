using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using Prism.Events;
using TrackerOfflineSearch.Events;

namespace TrackerOfflineSearch.ViewModels;

public class ToolsViewModel : ViewModelBase<ToolsViewModel>
{
    public ToolsViewModel(IEventAggregator eventAggregator, ILogger<ToolsViewModel> logger) : base(eventAggregator, logger)
    {
        this.LogDebug("{class} created", nameof(ToolsViewModel));

        this.EventAggregator.GetEvent<StartSearchEvent>().Subscribe(OnStartSearch, ThreadOption.UIThread);
        this.EventAggregator.GetEvent<SearchCompletedEvent>().Subscribe(OnSearchCompleted, ThreadOption.UIThread);
        
        this.Title = nameof(ToolsViewModel);
    }

    private void OnStartSearch(string query)
    {
    }

    private void OnSearchCompleted(string query)
    {
    }
}
