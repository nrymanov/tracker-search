using System.Reactive.Subjects;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.ViewModels;

public class MainWindowViewModel : ActivatableViewModel
{
    #region Constructor

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = """
        The constructor contains complex setup of reactive data streams (Rx.NET) for managing import progress state.
        Splitting into methods would reduce readability and understanding of data flow.
        """
        )]
    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IIndexService indexService,
        IBBTextConverter textConverter
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        _textConverter = textConverter ?? throw new ArgumentNullException(nameof(textConverter));

        _refreshSubject = new BehaviorSubject<bool>(value: true);

        #region Команды

        ImportCommand = ReactiveCommand.CreateFromTask<bool>(ExecuteImportAsync);
        ImportCommand.Subscribe(_refreshSubject);

        AboutCommand = ReactiveCommand.CreateFromTask(ShowAboutDialogAsync);

        SearchCommand = ReactiveCommand.CreateFromTask<PostQuery>(ExecuteSearchAsync);

        #endregion

        #region Список форумов
        //
        // Создаём список форумов и настраиваем его фильтрацию и сортировку
        //

        _forumCache = new (x => x.Id);

        var filter = this.WhenAnyValue(x => x.ForumFilterText, f => f?.Trim() ?? "")
            .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .Select(f => BuildForumFilterPredicate(f, _forumCache.Items));

        var forumSortComparer = SortExpressionComparer<ForumViewModel>.Ascending(f => f.Order).ThenByAscending(x => x.Name);

        _forumCache.Connect()
            .Filter(filter)
            .TransformToTree(x => x.ParentId)
            .Transform(x => new ForumViewModel(x, forumSortComparer))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _forumTree, forumSortComparer)
            .Subscribe();

        _refreshSubject.Where(success => success)
            .SelectMany(_ => Observable.FromAsync(LoadForumList))
            .Subscribe();

        #endregion

        #region Список документов
        //
        // Создадим список постов
        //

        _postCache = new(x => x.Id);

        var postSortComparer = SortExpressionComparer<Post>.Ascending(f => f.Index);
        _postCache.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _posts, postSortComparer)
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

        #endregion

        #region Поиск
        //
        // Настраиваем поиск
        //

        _currentQueryProperty = this.WhenAnyValue(x => x.SelectedForum, x => x.PostFilterText, (forum, post) => (ForumPath: forum?.Id, PostFilter: post?.Trim()))
            .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .Select(args => new PostQuery(args.PostFilter, args.PostFilter, args.ForumPath))
            .ToProperty(this, x => x.Query, new PostQuery());

        Observable.CombineLatest(
            this.WhenAnyValue(x => x.Query),
            _refreshSubject.Where(success => success),
            (q, _) => q
        )
            .Select(query => SearchCommand.Execute(query).Catch(Observable.Empty<Unit>()))
            .Switch()
            .Subscribe();

        #endregion

        SearchCommand.ThrownExceptions
            .Subscribe();

        this.WhenActivated(d =>
            Observable.FromAsync(LoadForumList)
                .Subscribe()
                .DisposeWith(d)
        );
    }

    #endregion

    #region Public

    //
    // Форумы
    //
    public string ForumFilterText
    {
        get => _forumFilterText;
        set => this.RaiseAndSetIfChanged(ref _forumFilterText, value);
    }

    public ReadOnlyObservableCollection<ForumViewModel> Forums => _forumTree;

    public ForumViewModel? SelectedForum
    {
        get => _selectedForum;
        set => this.RaiseAndSetIfChanged(ref _selectedForum, value);
    }

    //
    // Посты
    //
    public string PostFilterText
    {
        get => _postFilterText;
        set => this.RaiseAndSetIfChanged(ref _postFilterText, value);
    }

    public ReadOnlyObservableCollection<Post> Posts => _posts;

    public Post? SelectedPost
    {
        get => _selectedPost;
        set => this.RaiseAndSetIfChanged(ref _selectedPost, value);
    }

    public string SelectedPostContent => _postContentProperty.Value;

    public PostInfoViewModel? SelectedPostInfo => _selectedPostInfoProperty.Value;

    // Interaction

    public Interaction<Unit, bool> Import { get; } = new();

    public Interaction<Unit, Unit> About { get; } = new();

    // Команды

    public ReactiveCommand<Unit, bool> ImportCommand { get; }

    public ReactiveCommand<Unit, Unit> AboutCommand { get; }

    #endregion

    #region Private

    private PostQuery Query => _currentQueryProperty.Value;

    private ReactiveCommand<PostQuery, Unit> SearchCommand { get; }

    private Task LoadForumList(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var total = _indexService.TotalCount;

            var allForums = _indexService.GetForums().Concat([Forum.AllForums]);

            //
            // _forumCache используется для построения дерева форумов.
            // Для этого используется оператор TransformToTree в котором есть ошибка,
            // проявляющаяся когда одновременно удаляются, добавляются и изменяются элементы.
            // Как временное решение можно каждый раз грузить дерево заново.
            // Форумы читаются ьлдбко при старте и после импорта, т ч больших проблем из-за этого не возникнет.
            //
            _forumCache.Clear();
            _forumCache.EditDiff(allForums, (current, previous) => string.Equals(current.Id, previous.Id, StringComparison.Ordinal));
        }, ct);
    }

    private Task ExecuteSearchAsync(PostQuery query, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("Search begin {query}", query);

                var searchResults = _indexService.Search(query);

                ct.ThrowIfCancellationRequested();

                _postCache.EditDiff(searchResults.Items, (current, previous) => current.Id == previous.Id);

                _logger.LogDebug("Search end {query}", query);
            }
            catch (OperationCanceledException)
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

    private async Task<bool> ExecuteImportAsync()
    {
        var importCompleted = await Import.Handle(Unit.Default);
        if (importCompleted)
        {
            //_indexService.Refresh();
        }
        return importCompleted;
    }

    private async Task ShowAboutDialogAsync() => await About.Handle(Unit.Default);

    /// <summary>
    /// Создает фильтр для форумов, который включает форумы, совпадающие с критерием поиска, 
    /// а также все их родительские форумы в иерархии.
    /// </summary>
    /// <param name="forumFilter">Строка для поиска в названиях форумов. Если строка пуста, возвращается фильтр, принимающий все форумы.</param>
    /// <param name="forums">Коллекция форумов для применения фильтра.</param>
    /// <returns>Функция-предикат, возвращающая true для форумов, которые совпадают с фильтром или являются родителями совпадающих форумов.</returns>
    private static Func<Forum, bool> BuildForumFilterPredicate(string forumFilter, IEnumerable<Forum> forums)
    {
        var normalizedFilter = forumFilter.Trim();

        // Если фильтр не задан, возвращаем функцию, принимающую все форумы
        if (string.IsNullOrWhiteSpace(normalizedFilter))
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
            if (forum.Name.Contains(normalizedFilter, StringComparison.CurrentCultureIgnoreCase))
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
    private readonly IIndexService _indexService;
    private readonly IBBTextConverter _textConverter;

    private readonly SourceCache<Forum, string> _forumCache;
    private readonly ReadOnlyObservableCollection<ForumViewModel> _forumTree;
    private ForumViewModel? _selectedForum;

    private readonly SourceCache<Post, int> _postCache;
    private readonly ReadOnlyObservableCollection<Post> _posts;
    private Post? _selectedPost;
    private readonly ObservableAsPropertyHelper<PostInfoViewModel?> _selectedPostInfoProperty;
    private readonly ObservableAsPropertyHelper<string> _postContentProperty;

    private string _forumFilterText = "";
    private string _postFilterText = "";
    private readonly ObservableAsPropertyHelper<PostQuery> _currentQueryProperty;

    private readonly BehaviorSubject<bool> _refreshSubject;

    #endregion
}

