using System.Collections.ObjectModel;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using Prism.Events;
using TrackerOfflineSearch.Events;

namespace TrackerOfflineSearch.ViewModels;

public class HistoryViewModel : ViewModelBase<HistoryViewModel>
{
    public HistoryViewModel(IEventAggregator eventAggregator, ILogger<HistoryViewModel> logger) : base(eventAggregator, logger)
    {
        this.LogDebug("{class} created", nameof(HistoryViewModel));

        this.EventAggregator.GetEvent<StartSearchEvent>().Subscribe(OnStartSearch, ThreadOption.UIThread);
        this.EventAggregator.GetEvent<SearchCompletedEvent>().Subscribe(OnSearchCompleted, ThreadOption.UIThread);

        this.Title = nameof(HistoryViewModel);

        this.History.Add("Title:roger");
        this.History.Add("Title:roger Title:moore");
        this.History.Add("Title:avatar");
        this.History.Add("-Title:roger -Title:moore");
    }

    public ObservableCollection<string> History { get; } = new ObservableCollection<string>();

    private void OnStartSearch(string query)
    {
        this.History.Insert(0, query);
        while (this.History.Count > 5)
        { 
            this.History.RemoveAt(this.History.Count - 1);
        }
    }

    private void OnSearchCompleted(string query)
    { 
    }
}
