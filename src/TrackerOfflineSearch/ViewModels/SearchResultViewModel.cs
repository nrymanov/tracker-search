using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class SearchResultViewModel : ViewModelBase<SearchResultViewModel>
{
    #region Constructor
    
    public SearchResultViewModel(
        IPostRepository postRepository,
        IEventAggregator eventAggregator,
        ILogger<SearchResultViewModel> logger
        ) : base(eventAggregator, logger)
    {
        this.postRepository = postRepository ?? throw new System.ArgumentNullException(nameof(postRepository));

        this.EventAggregator.GetEvent<SearchActiveEvent>().Subscribe(this.OnSearch);

        this.postRepository
            .Connect()
            .Transform(post => new PostCellViewModel(post))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out this.items)
            .Subscribe(_ => this.SelectedPost = this.SearchResults.FirstOrDefault());

        this.WhenAnyValue(x => x.SelectedPost)
            .Select(vm => vm?.Post)
            .Subscribe(post => this.EventAggregator.GetEvent<PostSelectedEvent>().Publish(post));
    }

    #endregion

    #region Public properties & methods

    public bool SearchInProgress
    {
        get => this.searchInProgress;
        set => this.RaiseAndSetIfChanged(ref this.searchInProgress, value);
    }

    public ReadOnlyObservableCollection<PostCellViewModel> SearchResults => this.items;

    public PostCellViewModel? SelectedPost
    {
        get => this.selectedPost;
        set => this.RaiseAndSetIfChanged(ref this.selectedPost, value);
    }

    #endregion

    #region Private fields & methods
    private void OnSearch(bool isActive)
    {
#pragma warning disable CA2254 // Template should be a static expression
        this.Logger.LogDebug(isActive ? "Search started" : "Search completed");
#pragma warning restore CA2254 // Template should be a static expression
        this.SearchInProgress = isActive;
    }

    private readonly IPostRepository postRepository;
    private readonly ReadOnlyObservableCollection<PostCellViewModel> items;
    private PostCellViewModel? selectedPost;
    private bool searchInProgress;

    #endregion
}
