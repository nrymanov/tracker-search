using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class SearchViewModel : ViewModelBase<SearchViewModel>
{
    private readonly IQueryBuilder _queryBuilder;
    private string? _queryString;
    private bool _showTips;

    public SearchViewModel(IQueryBuilder queryBuilder, IEventAggregator eventAggregator, ILogger<SearchViewModel> logger) : base(eventAggregator, logger)
    {
        this.LogDebug("{class} created", nameof(SearchViewModel));

        this._queryBuilder = queryBuilder ?? throw new System.ArgumentNullException(nameof(queryBuilder));

        this.Title = nameof(SearchViewModel);

        var canSearch = this
            .WhenAnyValue(vm => vm.QueryString)
            .Select(term => !string.IsNullOrWhiteSpace(term));

        this.SearchCommand = ReactiveCommand.Create(StartSearch, canSearch);
        this.SwitchSearchTipsCommand = ReactiveCommand.Create(() => { this.ShowTips = !this.ShowTips; });
    }

    public string? QueryString
    {
        get => _queryString;
        set => this.RaiseAndSetIfChanged(ref _queryString, value);
    }

    public ReactiveCommand<Unit, Unit> SearchCommand { get; }

    public bool ShowTips
    {
        get => _showTips;
        set => this.RaiseAndSetIfChanged(ref _showTips, value);
    }

    public ReactiveCommand<Unit, Unit> SwitchSearchTipsCommand { get; }

    private void StartSearch()
    {
        if (this._queryBuilder.TryBuild(this.QueryString!, out var query))
        {            
            this.EventAggregator.GetEvent<StartSearchEvent>().Publish(query!.ToString());
        }
    }
}
