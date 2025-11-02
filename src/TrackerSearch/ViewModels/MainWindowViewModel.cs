using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerSearch.ViewModels;

public class MainWindowViewModel : ActivatableViewModel
{
    #region Constructor

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "<Pending>")]
    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IIndexSearchService searchService,
        IBBTextConverter textConverter
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _textConverter = textConverter ?? throw new ArgumentNullException(nameof(textConverter));

        _import = new();

        ImportCommand = ReactiveCommand.CreateFromTask<bool>(ImportAsync);

        _about = new();

        AboutCommand = ReactiveCommand.CreateFromTask(AboutAsync);

        //
        // Создадим список форумов и настроим его фильтрацию и сортировку
        //
        _forumCache = new (x => x.Id);

        var filter = this.WhenAnyValue(x => x.ForumFilter, (string? f) => f?.Trim() ?? "")
            .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .Select(f => CreateFilter(f, _forumCache.Items));

        var forumComparer = SortExpressionComparer<ForumViewModel>.Ascending(f => f.Order).ThenByAscending(x => x.Name);

        _forumCache.Connect()
            .Filter(filter)
            .TransformToTree(x => x.ParentId)
            .Transform(x => new ForumViewModel(x, forumComparer))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _forumTreeItems, forumComparer)
            .Subscribe();

        //
        // Создадим список постов
        //
        _postCache = new(x => x.Id);

        var postComparer = SortExpressionComparer<Post>.Ascending(f => f.Index);
        _postCache.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _postItems, postComparer)
            .Subscribe();

        _selectedPostInfoProperty = this.WhenAnyValue(x => x.SelectedPost)
            .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .Select(post => post is null ? null : new PostInfoViewModel(post))
            .ToProperty(this, x => x.SelectedPostInfo);

        _postContentProperty = this.WhenAnyValue(x => x.SelectedPost)
            .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .Select(post => post is null ? "" : _textConverter.Convert(post.Content))
            .ToProperty(this, x => x.SelectedPostContent);

        //
        // Займемся поиском
        //
        SearchCommand = ReactiveCommand.CreateFromTask<PostQuery>(SearchAsync);

        this.WhenAnyValue(x => x.SelectedForum, x => x.PostFilter, (forum, post) => (ForumPath: forum?.Id, PostFilter: post?.Trim()))
            .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .Select(args => new PostQuery(args.PostFilter, args.PostFilter, args.ForumPath))
            .Select(query => SearchCommand.Execute(query).Catch(Observable.Empty<Unit>()))
            .Switch()
            .Subscribe();

        SearchCommand.ThrownExceptions
            .Subscribe();

        this.WhenActivated(d =>
        {
            Observable.FromAsync(InitAsync)
                .Subscribe()
                .DisposeWith(d);
        });
    }

    #endregion

    #region Public

    //
    // Форумы
    //
    public string ForumFilter
    {
        get => _forumFilter;
        set => this.RaiseAndSetIfChanged(ref _forumFilter, value);
    }

    public ReadOnlyObservableCollection<ForumViewModel> Forums => _forumTreeItems;

    public ForumViewModel? SelectedForum
    {
        get => _selectedForum;
        set => this.RaiseAndSetIfChanged(ref _selectedForum, value);
    }

    //
    // Посты
    //
    public string PostFilter
    {
        get => _postFilter;
        set => this.RaiseAndSetIfChanged(ref _postFilter, value);
    }

    public ReadOnlyObservableCollection<Post> Posts => _postItems;

    public Post? SelectedPost
    {
        get => _selectedPost;
        set => this.RaiseAndSetIfChanged(ref _selectedPost, value);
    }

    public string SelectedPostContent => _postContentProperty.Value;

    public PostInfoViewModel? SelectedPostInfo => _selectedPostInfoProperty.Value;

    // Команды

    public Interaction<Unit, bool> Import => _import;

    public ReactiveCommand<Unit, bool> ImportCommand { get; }

    public  Interaction<Unit, Unit> About => _about;

    public ReactiveCommand<Unit, Unit> AboutCommand { get; }

    #endregion

    #region Private

    private ReactiveCommand<PostQuery, Unit> SearchCommand { get; }

    private Task InitAsync(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var total = _searchService.TotalCount;

            var forums = GetForumWithAncestors(_searchService.GetForums()).Concat([Forum.AllForums]);

            _forumCache.EditDiff(forums, (current, prevous) => string.Equals(current.Id, prevous.Id, StringComparison.Ordinal));
        }, ct);
    }

    private Task SearchAsync(PostQuery query, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("Search begin {query}", query);

                var searchResult = _searchService.Search(query);

                ct.ThrowIfCancellationRequested();

                _postCache.EditDiff(searchResult.Items, (current, prevous) => current.Id == prevous.Id);

                _logger.LogDebug("Search end {query}", query);
            }
            catch (TaskCanceledException)
            {
                // Ignore
                _logger.LogDebug("Search cancelled {query}", query);
            }
            catch (Exception err)
            {
                // Ignore
                _logger.LogError(err, "Search error {query}", query);
            }
        }, ct);
    }

    private async Task<bool> ImportAsync()
    {
        var importCompleted = await _import.Handle(Unit.Default);
        //if (importCompleted)
        {
            _searchService.Refresh();
        }
        return importCompleted;
    }

    private async Task AboutAsync() => await _about.Handle(Unit.Default);

    /// <summary>
    /// Возвращает коллекцию форумов, включая всех их предков (родительские форумы) до корневого уровня.
    /// Для отсутствующих в исходной коллекции предков создаются новые элементы.
    /// </summary>
    /// <param name="forums">Исходная коллекция форумов</param>
    /// <returns>Коллекция форумов, содержащая исходные форумы и всех их предков до корневого уровня</returns>
    private static IReadOnlyCollection<Forum> GetForumWithAncestors(IEnumerable<Forum> forums)
    {
        var result = forums.ToDictionary(x => x.Id, StringComparer.Ordinal);

        foreach (var forum in forums)
        {
            var parentId = forum.ParentId;
            while (!string.IsNullOrEmpty(parentId) && !result.ContainsKey(parentId))
            {
                var parent = new Forum(parentId);
                result[parentId] = parent;
                parentId = parent.ParentId;
            }
        }

        return result.Values;
    }

    /// <summary>
    /// Создает фильтр для форумов, который включает форумы, совпадающие с критерием поиска, 
    /// а также всех их родительские форумы в иерархии.
    /// </summary>
    /// <param name="forumFilter">Строка для поиска в названиях форумов. Если строка пустая, возвращается фильтр, принимающий все форумы.</param>
    /// <param name="forums">Коллекция форумов для применения фильтра.</param>
    /// <returns>Функция-предикат, возвращающая true для форумов, которые совпадают с фильтром или являются родителями совпадающих форумов.</returns>
    private static Func<Forum, bool> CreateFilter(string forumFilter, IEnumerable<Forum> forums)
    {
        // Если фильтр не задан, возвращаем функцию, принимающую все элементы
        if (string.IsNullOrWhiteSpace(forumFilter))
        {
            return _ => true;
        }

        // Создаем словарь для быстрого поиска форумов по ID
        var allForums = forums.ToDictionary(x => x.Id, StringComparer.Ordinal);

        // Словарь для хранения форумов, прошедших фильтрацию (сами совпадающие и их родители)
        var matchingForums = new Dictionary<string, Forum>(StringComparer.Ordinal);

        // Очередь для обхода иерархии родителей
        var queue = new Queue<Forum>();

        // Первый этап: находим форумы, названия которых содержат искомую строку
        foreach (var forum in allForums.Values)
        {
            if (forum.Name.Contains(forumFilter, StringComparison.CurrentCultureIgnoreCase))
            {
                matchingForums[forum.Id] = forum;
                queue.Enqueue(forum);
            }
        }

        // Второй этап: поднимаемся по иерархии родителей для каждого совпадающего форума
        while (queue.Count > 0)
        {
            var forum = queue.Dequeue();

            // Если у форума есть родитель и он еще не добавлен в результат
            if (allForums.TryGetValue(forum.ParentId, out var parentForum) && matchingForums.TryAdd(parentForum.Id, parentForum))
            {
                // Добавляем родителя в очередь для проверки его родителей
                queue.Enqueue(parentForum);
            }
        }

        return forum => matchingForums.ContainsKey(forum.Id);
    }

    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IIndexSearchService _searchService;
    private readonly IBBTextConverter _textConverter;

    private readonly SourceCache<Forum, string> _forumCache;
    private readonly ReadOnlyObservableCollection<ForumViewModel> _forumTreeItems;
    private ForumViewModel? _selectedForum;

    private readonly SourceCache<Post, int> _postCache;
    private readonly ReadOnlyObservableCollection<Post> _postItems;
    private Post? _selectedPost;
    private readonly ObservableAsPropertyHelper<PostInfoViewModel?> _selectedPostInfoProperty;
    private readonly ObservableAsPropertyHelper<string> _postContentProperty;

    private string _forumFilter = "";
    private string _postFilter = "";

    private readonly Interaction<Unit, bool> _import;
    private readonly Interaction<Unit, Unit> _about;

    #endregion
}

