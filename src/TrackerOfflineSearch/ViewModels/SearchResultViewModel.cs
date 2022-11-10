using Microsoft.Extensions.Logging;
using Prism.Events;

namespace TrackerOfflineSearch.ViewModels;

public class SearchResultViewModel : ViewModelBase<SearchResultViewModel>
{
    public SearchResultViewModel(IEventAggregator eventAggregator, ILogger<SearchResultViewModel> logger) : base(eventAggregator, logger)
    {
        this.LogDebug("{class} created", nameof(SearchResultViewModel));
        this.Title = nameof(SearchResultViewModel);
    }
}
