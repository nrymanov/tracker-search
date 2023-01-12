using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;
using ReactiveUI;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.ViewModels;

namespace TrackerOfflineSearch.ForumSelector;

public class ForumSelectorViewModel : ViewModelBase<ForumSelectorViewModel>, IDialogAware
{
    #region Constructor

    public ForumSelectorViewModel(
        IPostRepository repository,
        IEventAggregator eventAggregator,
        ILogger<ForumSelectorViewModel> logger
        ) : base(eventAggregator, logger)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

        this.Title = "Select Forum";

        this.EventAggregator
            .GetEvent<ForumSelectedEvent>()
            .Subscribe(this.OnForumSelected);

        var canClose = this.WhenAnyValue(x => x.SelectedPath).Select(sp => !string.IsNullOrEmpty(sp));
        this.SelectCommand = ReactiveCommand.Create(() => this.RaiseRequestClose(this.SelectedPath), canClose);

        this.ClearCommand = ReactiveCommand.Create(() => this.RaiseRequestClose(null));

        this.forumList
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out this.items)
            .Subscribe();

        this.WhenAnyValue(x => x.Filter)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Subscribe(this.UpdateItemList);
    }

    #endregion

    #region IDialogAware implementation

    public void OnDialogOpened(IDialogParameters parameters)
    {
        this.SelectedPath = parameters.GetValue<string>(nameof(this.SelectedPath));

        this.allForums.AddRange(this.repository.Forums);
        this.UpdateItemList(null);
    }

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public event Action<IDialogResult> RequestClose;

    #endregion

    #region Public properties & methods

    public ReadOnlyObservableCollection<Forum> Items => this.items;

    public string SelectedPath
    {
        get => this.selectedPath;
        set => this.RaiseAndSetIfChanged(ref this.selectedPath, value);
    }

    public string Filter
    {
        get => this.filter;
        set => this.RaiseAndSetIfChanged(ref this.filter, value);
    }

    public ReactiveCommand<Unit, Unit> SelectCommand { get; }

    public ReactiveCommand<Unit, Unit> ClearCommand { get; }

    #endregion

    #region Private fields & methods

    public class ForumSelectedEvent : PubSubEvent<(string, bool)> { }

    [DebuggerDisplay("Name = {Name}, Path = {Path}")]
    public class Forum : ReactiveObject
    {
        public const string PathSeparator = " - ";

        public Forum(string name, Forum parent, IEventAggregator eventAggregator)
        {
            this.Name = name;
            this.Parent = parent;
            this.eventAggregator = eventAggregator;
        }

        public string Name { get; }

        public Forum? Parent { get; }

        public List<Forum> Children { get; } = new List<Forum>();

        public string Path
        {
            get
            {
                if (this.Parent is null)
                    return this.Name;

                return $"{this.Parent.Path} - {this.Name}";
            }
        }

        public bool IsSelected
        {
            get => this.isSelected;
            set
            {
                if (this.isSelected == value)
                    return;

                this.isSelected = value;

                this.eventAggregator.GetEvent<ForumSelectedEvent>().Publish((this.Path, value));

                this.RaisePropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => this.isExpanded;
            set => this.RaiseAndSetIfChanged(ref this.isExpanded, value);
        }

        private bool isSelected;
        private bool isExpanded;
        private readonly IEventAggregator eventAggregator;
    }

    private IEnumerable<Forum> BuildForumTree(IEnumerable<string> forumNames, bool expandTree)
    {
        var forumCache = new Dictionary<string, Forum>();

        var topForums = new List<Forum>();

        foreach (var fn in forumNames)
        {
            var parts = fn.Split(Forum.PathSeparator);

            Forum? parentForum = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var f = new Forum(parts[i], parentForum, this.EventAggregator) { IsExpanded = expandTree };
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

                    f.IsSelected = f.Path == this.SelectedPath;
                    forumCache[f.Path] = f;
                    parentForum = f;
                }
            }
        }

        return topForums;
    }

    private void UpdateItemList(string filter)
    {
        static bool NameContains(string forumPath, string filter) 
        {
            var start = forumPath.LastIndexOf(Forum.PathSeparator);
            if (start < 0)
            {
                return forumPath.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }

            return forumPath.IndexOf(filter, start + Forum.PathSeparator.Length, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        IEnumerable<string> filtered = this.allForums;

        var hasFilter = !string.IsNullOrWhiteSpace(filter);

        if (hasFilter)
            filtered = filtered.Where(f => NameContains(f, filter));
        
        var tree = this.BuildForumTree(filtered, hasFilter);

        this.forumList.Edit(
            innerList => 
            {
                innerList.Clear();
                innerList.AddRange(tree);
            });
    }

    private void OnForumSelected((string, bool) si)
    {
        var (forumPath, selected) = si;

        if (selected)
        {
            this.SelectedPath = forumPath;
        }
        else
        {
            if (this.SelectedPath == forumPath)
                this.SelectedPath = null;
        }
    }

    private void RaiseRequestClose(string selectedPath)
    {
        var parameters = new DialogParameters { { nameof(this.SelectedPath), selectedPath } };
        var dialogResult = new DialogResult(ButtonResult.OK, parameters);
        this.RequestClose?.Invoke(dialogResult);
    }

    private readonly IPostRepository repository;
    private readonly List<string> allForums = new();
    private readonly SourceList<Forum> forumList = new();
    private readonly ReadOnlyObservableCollection<Forum> items;

    private string selectedPath;
    private string filter;

    #endregion
}
