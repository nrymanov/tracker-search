using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class QueryEditorViewModel : ViewModelBase<QueryEditorViewModel>
{
    #region Constructor

    public QueryEditorViewModel(
        IQueryBuilder queryBuilder,
        IPostRepository postRepository,
        IEventAggregator eventAggregator,
        ILogger<QueryEditorViewModel> logger
        ) : base(eventAggregator, logger)
    {
        this._queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
        this._postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));

        this.Forums = new ObservableCollection<string>(this._postRepository.Forums);

        this.ChangeQueryTypeCommand = ReactiveCommand.Create(() =>
        {
            this.IsAdvancedQuery = !this.IsAdvancedQuery;
            GC.Collect();
        });

        this.WhenAnyValue(
            vm => vm.TitleFilter, vm => vm.ContentFilter,
            vm => vm.ForumFilter,
            vm => vm.FromSizeFilter, vm => vm.ToSizeFilter,
            vm => vm.FromDateFilter, vm => vm.ToDateFilter
            )
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(qs => this._queryBuilder.TryBuild(
                new PostQuery(
                    this.TitleFilter, this.ContentFilter,
                    this.ForumFilter,
                    this.FromSizeFilter, this.ToSizeFilter,
                    this.FromDateFilter, this.ToDateFilter
                ),
                out var query
                ) ? query : null)
            .Where(q => q != null)
            .DistinctUntilChanged()
            .Subscribe(q => this._postRepository.Search(q!));
    }

    #endregion

    #region Public properties & methods

    public string? TitleFilter
    {
        get => _titleFilter;
        set => this.RaiseAndSetIfChanged(ref _titleFilter, value);
    }
    public string? ContentFilter
    {
        get => _contentFilter;
        set => this.RaiseAndSetIfChanged(ref _contentFilter, value);
    }

    public string? ForumFilter
    {
        get => _forumFilter;
        set => this.RaiseAndSetIfChanged(ref _forumFilter, value);
    }

    public long? FromSizeFilter
    {
        get => _fromSizeFilter;
        set => this.RaiseAndSetIfChanged(ref _fromSizeFilter, value);
    }
    public long? ToSizeFilter
    {
        get => _toSizeFilter;
        set => this.RaiseAndSetIfChanged(ref _toSizeFilter, value);
    }

    public DateTime? FromDateFilter
    {
        get => _fromDateFilter;
        set => this.RaiseAndSetIfChanged(ref _fromDateFilter, value);
    }
    public DateTime? ToDateFilter
    {
        get => _toDateFilter;
        set => this.RaiseAndSetIfChanged(ref _toDateFilter, value);
    }

    public ObservableCollection<string> Forums 
    {
        get;
    }

    public bool IsAdvancedQuery
    {
        get => _isAdvancedQuery;
        set => this.RaiseAndSetIfChanged(ref _isAdvancedQuery, value);
    }

    public ReactiveCommand<Unit, Unit> ChangeQueryTypeCommand { get; }

    #endregion

    #region Private fields & methods

    private readonly IQueryBuilder _queryBuilder;
    private readonly IPostRepository _postRepository;

    private string? _titleFilter;
    private string? _contentFilter;

    private string? _forumFilter;

    private long? _fromSizeFilter;
    private long? _toSizeFilter;

    private DateTime? _fromDateFilter;
    private DateTime? _toDateFilter;
    
    private bool _isAdvancedQuery;

    #endregion
}
