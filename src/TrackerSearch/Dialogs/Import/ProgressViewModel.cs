using System.Diagnostics;
using System.Reactive.Subjects;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ProgressViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ProgressViewModel(
        IScreen screen,
        IArchiveReader archiveReader,
        IIndexService indexService
        )
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));
        _archiveReader = archiveReader ?? throw new ArgumentNullException(nameof(archiveReader));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));

        ImportCommand = ReactiveCommand.CreateFromTask<ImportParameters, ImportResult>(ImportAsync);
        CancelCommand = ReactiveCommand.Create(() => { });

        _messageProperty = _progressMessageSubject
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.Message);

        //
        // Пока команда не работает - пропускаем события и возвращаем константу.
        // Когда команда выполняется, переключаемся на таймер и слушаем его до тех пор,
        // пока команда не просигналет об окончании выполнения.
        //
        _elapsedProperty =
            Observable.Concat(
                // Фаза 1: Ожидание начала выполнения
                ImportCommand.IsExecuting
                    .TakeWhile(isRunning => !isRunning)
                    .Select(_ => TimeSpan.Zero),

                // Фаза 2: Таймер во время выполнения
                Observable.Interval(TimeSpan.FromSeconds(1)).StartWith(-1)
                    .TakeUntil(ImportCommand.IsExecuting.Where(isRunning => !isRunning))
                    .Select(i => TimeSpan.FromSeconds(i + 1))
                )
            .ObserveOn(RxApp.MainThreadScheduler)
            //.Do(elapsed => Debug.WriteLine($"Updating Elapsed: {elapsed}"), () => Debug.WriteLine("Updating Elapsed: COMPLETED"))
            .ToProperty(this, x => x.Elapsed, deferSubscription: true);

        _infoTipProperty = 
            Observable.Concat(
                ImportCommand.IsExecuting
                    .TakeWhile(isRunning => !isRunning)
                    .Select(_ => GetTip(0)),

                Observable.Interval(TimeSpan.FromSeconds(10)).StartWith(-1)
                    .TakeUntil(ImportCommand.IsExecuting.Where(isRunning => !isRunning))
                    .Select(idx => GetTip(idx + 1))
                )
            .ObserveOn(RxApp.MainThreadScheduler)
            //.Do(tip => Debug.WriteLine($"Updating tip: {tip}"), () => Debug.WriteLine("Updating tip: COMPLETED"))
            .ToProperty(this, x => x.InfoTip);

        ImportCommand.ThrownExceptions
            .Subscribe();

        this.WhenActivated(d =>
        {
            if (_parameters is null)
            {
                throw new InvalidOperationException("Page is not initialized properly!");
            }

            Observable.Return(_parameters)
                .InvokeCommand(ImportCommand)
                .DisposeWith(d);
        });
    }

    #region IRoutableViewModel

    public string? UrlPathSegment => "import-progress";

    public IScreen HostScreen { get; }

    #endregion

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public async Task<bool> ConfirmCancelAsync()
    {
        using var p = PauseImport();

        return await ConfirmCancel.Handle(Unit.Default);
    }

    #endregion

    #region Public

    public ProgressViewModel WithParameters(ImportParameters parameters)
    {
        _parameters = parameters;
        return this;
    }

    public ReactiveCommand<ImportParameters, ImportResult> ImportCommand { get; }

    public string Message => _messageProperty.Value;

    public TimeSpan Elapsed => _elapsedProperty.Value;

    public string InfoTip => _infoTipProperty.Value;

    // Interaction

    public Interaction<Unit, bool> ConfirmCancel { get; } = new();

    #endregion

    #region Private

    private IDisposable PauseImport()
    {
        _pauseEvent.Reset();
        return Disposable.Create(_pauseEvent.Set);
    }

    private async Task<int> ImportDocumentsAsync(IIndexWriterSession writerSession, string archivePath, CancellationToken ct)
    {
        int total = 0;
        var lastTime = Stopwatch.StartNew();

        var oneSecond = TimeSpan.FromSeconds(1);

        _progressMessageSubject.OnNext($"Импортировано {0:N0} документов");

        await Parallel.ForEachAsync(
            _archiveReader.ReadPostsAsync(archivePath, ct),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            (post, token) =>
            {
                // Если стоит на паузе — блокируем выполнение
                _pauseEvent.Wait(token);

                writerSession.Add(post);

                int count = Interlocked.Increment(ref total);

                if (lastTime.Elapsed >= oneSecond)
                {
                    lastTime.Restart();
                    _progressMessageSubject.OnNext($"Импортировано {count:N0} документов");
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        _progressMessageSubject.OnNext($"Импортировано {total:N0} документов");

        return total;
    }

    private async Task<ImportResult> ImportAsync(ImportParameters parameters, CancellationToken ct)
    {
        try
        {
            using var session = _indexService.OpenWriterSession();

            var sw = Stopwatch.StartNew();

            await session.ClearAsync(ct).ConfigureAwait(false);

            var documentCount = await ImportDocumentsAsync(session, parameters.ArchivePath, ct).ConfigureAwait(false);

            _progressMessageSubject.OnNext("Оптимизация индекса");
            await session.OptimizeAsync(parameters.IndexOptimization, ct).ConfigureAwait(false);

            _progressMessageSubject.OnNext("Завершение импорта");
            await session.CommitAsync(ct).ConfigureAwait(false);

            _progressMessageSubject.OnNext("Импорт завершен");

            return new ImportCompletedResult(parameters, documentCount, sw.Elapsed);
        }
        catch (Exception err)
        {
            //if (err is OperationCanceledException)
            //{
            //    throw;
            //}

            return new ImportFailedResult(parameters, err);
        }
    }

    private static readonly string[] InfoTips = [
        "Во время выпонения импорта размер индекса может удваиваться в зависимости от выбранной стратегии оптимизации.",
        "Чтобы найти точную фразу, заключите ее в кавычки. Например: \"белое солнце\"",
        "Чтобы слово обязательно было в результатах, поставьте перед ним +. Например: +рецепт яблочный пирог",
        "Чтобы исключить слово из поиска, поставьте перед ним -. Например: яблоко -сок (найдется про яблоки, но не про сок)",
        "Чтобы найти любое из нескольких слов, используйте OR (или). Например: кот OR собака OR хомяк",
        "Чтобы искать с учетом опечаток, добавьте ~ в конец слова. Например: шоколад~ (найдет \"шаколад\", \"шоколат\")",
        "Чтобы найти слова, которые находятся рядом, используйте ~ после фразы. Например: \"быстрая доставка\"~3 (слова в пределах 3 слов друг от друга)",
        "Чтобы найти слова по шаблону, используйте * и ?. Например: к*т (найдет \"кот\", \"кристалл\", \"квест\")"
        ];

    private static string GetTip(long index)
    {
        var tipIndex = index % InfoTips.Length;

        return InfoTips[tipIndex];
    }

    private readonly IArchiveReader _archiveReader;
    private readonly IIndexService _indexService;
    private readonly ObservableAsPropertyHelper<string> _messageProperty;
    private readonly ObservableAsPropertyHelper<TimeSpan> _elapsedProperty;
    private readonly Subject<string> _progressMessageSubject = new();
    private readonly ManualResetEventSlim _pauseEvent = new(initialState: true);

    private ImportParameters? _parameters;
    private readonly ObservableAsPropertyHelper<string> _infoTipProperty;

    #endregion
}
