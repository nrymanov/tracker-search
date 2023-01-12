using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;
using ReactiveUI;
using TrackerOfflineSearch.ForumSelector;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class QueryEditorViewModel : ViewModelBase<QueryEditorViewModel>
{
    #region Constructor

    public QueryEditorViewModel(
        IQueryBuilder queryBuilder,
        IPostRepository postRepository,
        IDialogService dialogService,
        IEventAggregator eventAggregator,
        ILogger<QueryEditorViewModel> logger
        ) : base(eventAggregator, logger)
    {
        this.queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
        this.postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        this.Intervals = new ObservableCollection<DateIntervalViewModel> 
        { 
            new DateIntervalViewModel(DateIntervalKind.None),
            new DateIntervalViewModel(DateIntervalKind.Week),
            new DateIntervalViewModel(DateIntervalKind.TwoWeeks),
            new DateIntervalViewModel(DateIntervalKind.Month),
            new DateIntervalViewModel(DateIntervalKind.Quarter),
            new DateIntervalViewModel(DateIntervalKind.HalfYear),
            new DateIntervalViewModel(DateIntervalKind.Year)
        };

        this.SelectedInterval = this.Intervals.First();

        this.SelectForumCommand = ReactiveCommand.Create(this.SelectForumExecute);

        this.ClearForumCommand = ReactiveCommand.Create(() => { this.ForumFilter = null; });

        this.ChangeQueryTypeCommand = ReactiveCommand.Create(() =>
        {
            this.IsAdvancedQuery = !this.IsAdvancedQuery;
            GC.Collect(); // TODO 10 remove this!!!
        });

        this.WhenAnyValue(
            vm => vm.TitleFilter, vm => vm.ContentFilter,
            vm => vm.ForumFilter,
            vm => vm.FromSizeFilter, vm => vm.ToSizeFilter,
            vm => vm.SelectedInterval
            )
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(qs => this.queryBuilder.TryBuild(
                new PostQuery(
                    this.TitleFilter, this.ContentFilter,
                    this.ForumFilter,
                    this.FromSizeFilter, this.ToSizeFilter,
                    this.SelectedInterval
                ),
                out var query
                ) ? query : null)
            .Where(q => q != null)
            .DistinctUntilChanged()
            .Subscribe(q => this.postRepository.Search(q!));
    }

    #endregion

    #region Public properties & methods

    public string? TitleFilter
    {
        get => this.titleFilter;
        set => this.RaiseAndSetIfChanged(ref this.titleFilter, value);
    }

    public string? ContentFilter
    {
        get => this.contentFilter;
        set => this.RaiseAndSetIfChanged(ref this.contentFilter, value);
    }

    public string? ForumFilter
    {
        get => this.forumFilter;
        set => this.RaiseAndSetIfChanged(ref this.forumFilter, value);
    }

    public long? FromSizeFilter
    {
        get => this.fromSizeFilter;
        set => this.RaiseAndSetIfChanged(ref this.fromSizeFilter, value);
    }
    
    public long? ToSizeFilter
    {
        get => this.toSizeFilter;
        set => this.RaiseAndSetIfChanged(ref this.toSizeFilter, value);
    }

    public ObservableCollection<DateIntervalViewModel> Intervals
    { 
        get;
    }

    public DateIntervalViewModel SelectedInterval 
    {
        get => this.selectedInterval;
        set => this.RaiseAndSetIfChanged(ref this.selectedInterval, value);
    }

    public bool IsAdvancedQuery
    {
        get => this.isAdvancedQuery;
        set => this.RaiseAndSetIfChanged(ref this.isAdvancedQuery, value);
    }

    public ReactiveCommand<Unit, Unit> SelectForumCommand { get; }

    public ReactiveCommand<Unit, Unit> ClearForumCommand { get; }

    public ReactiveCommand<Unit, Unit> ChangeQueryTypeCommand { get; }

    #endregion

    #region Private fields & methods

    private void SelectForumExecute()
    {
        this.dialogService.ShowSelectForumDialog(this.ForumFilter, ff => this.ForumFilter = ff);
    }

    private readonly IQueryBuilder queryBuilder;
    private readonly IPostRepository postRepository;
    private readonly IDialogService dialogService;
    private string? titleFilter;
    private string? contentFilter;

    private string? forumFilter;

    private long? fromSizeFilter;
    private long? toSizeFilter;

    private DateIntervalViewModel selectedInterval;

    private bool isAdvancedQuery;

    #endregion
}
