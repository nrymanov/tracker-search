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
    public SearchResultViewModel(
        IPostRepository postRepository,
        IEventAggregator eventAggregator,
        ILogger<SearchResultViewModel> logger
        ) : base(eventAggregator, logger)
    {
        this._postRepository = postRepository ?? throw new System.ArgumentNullException(nameof(postRepository));

        this._postRepository
            .Connect()
            .Transform(post => new PostCellViewModel(post))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out this._items)
            .Subscribe(_ => {
                this.SelectedPost = this.SearchResults.FirstOrDefault();
            });

        this.WhenAnyValue(x => x.SelectedPost)
            .Subscribe(post => {
                this.EventAggregator.GetEvent<PostSelectedEvent>().Publish(post?.Post);
            });
    }

    private readonly IPostRepository _postRepository;
    private readonly ReadOnlyObservableCollection<PostCellViewModel> _items;
    private PostCellViewModel? selectedPost;

    public ReadOnlyObservableCollection<PostCellViewModel> SearchResults => _items;

    public PostCellViewModel? SelectedPost
    {
        get => selectedPost;
        set
        {
            this.RaiseAndSetIfChanged(ref this.selectedPost, value);
        }
    }
}
