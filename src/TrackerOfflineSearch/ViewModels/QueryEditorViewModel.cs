using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
        this.queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
        this.postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));

        this.Forums = new ObservableCollection<string>(GetToplevelForums(this.postRepository.Forums));

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

    public ObservableCollection<string> Forums 
    {
        get;
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

    public ReactiveCommand<Unit, Unit> ChangeQueryTypeCommand { get; }

    #endregion

    #region Private fields & methods

    [DebuggerDisplay("Name = {Name} Path = {Path}")]
    private class Forum
    {
        public string Name { get; init; }

        public Forum? Parent { get; init; }

        public string Path 
        {
            get 
            {
                if (this.Parent is null)
                    return this.Name;

                return $"{this.Parent.Path} - {this.Name}";
            }
        }

        public List<Forum> Children { get; } = new List<Forum>();
    }

    private static IEnumerable<Forum> BuildForumTree(IEnumerable<string> forums)
    {
        var forumCache = new Dictionary<string, Forum>();

        var topForums = new List<Forum>();

        foreach (var fn in forums)
        {
            var parts = fn.Split(" - ");

            Forum? parentForum = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var f = new Forum { Name = parts[i], Parent = parentForum };
                if (forumCache.TryGetValue(f.Path, out var cached))
                {
                    parentForum = cached;
                }
                else
                {
                    if (parentForum is null)
                        topForums.Add(f);
                    else
                        parentForum.Children.Add(f);
                    forumCache[f.Path] = f;
                    parentForum = f;
                }
            }
        }

        return topForums;
    }

    private static IEnumerable<string> GetToplevelForums(IEnumerable<string> forums) => BuildForumTree(forums).Select(f => f.Name);

    private readonly IQueryBuilder queryBuilder;
    private readonly IPostRepository postRepository;

    private string? titleFilter;
    private string? contentFilter;

    private string? forumFilter;

    private long? fromSizeFilter;
    private long? toSizeFilter;

    private DateIntervalViewModel selectedInterval;

    private bool isAdvancedQuery;

    #endregion
}
