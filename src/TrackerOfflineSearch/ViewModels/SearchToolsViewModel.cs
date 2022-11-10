using Microsoft.Extensions.Logging;
using Prism.Events;

namespace TrackerOfflineSearch.ViewModels;

public class SearchToolsViewModel : ViewModelBase<SearchToolsViewModel>
{
    public SearchToolsViewModel(IEventAggregator eventAggregator, ILogger<SearchToolsViewModel> logger) : base(eventAggregator, logger)
    {
        this.LogDebug("{class} created", nameof(SearchToolsViewModel));
        this.Title = nameof(SearchToolsViewModel);
    }
}