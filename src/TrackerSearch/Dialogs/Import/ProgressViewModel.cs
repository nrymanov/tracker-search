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

        _messageProperty = _messages
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.Message);

        var start = DateTimeOffset.Now;

        _elapsedProperty = Observable.CombineLatest(
                Observable.Create<DateTimeOffset>(o =>
                {
                    o.OnNext(DateTimeOffset.Now);
                    o.OnCompleted();
                    return Disposable.Empty;
                }),
                Observable.Interval(TimeSpan.FromSeconds(1)).StartWith(0).Select(_ => DateTimeOffset.Now),
                (start, now) => TimeSpan.FromSeconds(Math.Floor((DateTimeOffset.Now - start).TotalSeconds))
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .TakeUntil(ImportCommand)
            .ToProperty(this, x => x.Elapsed, deferSubscription: true);

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

    // Ask confirmation
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

    // Interaction

    public Interaction<Unit, bool> ConfirmCancel { get; } = new();

    #endregion

    #region Private

    private IDisposable PauseImport()
    {
        _pauseEvent.Reset();
        return Disposable.Create(_pauseEvent.Set);
    }

    private async Task<int> ImportDocumentsAsync(ImportParameters parameters, CancellationToken ct)
    {
        int total = 0;
        var lastTime = Stopwatch.StartNew();

        var oneSecond = TimeSpan.FromSeconds(1);

        _messages.OnNext($"Импортировано {0:N0} документов");

        await Parallel.ForEachAsync(
            _archiveReader.ReadPostsAsync(parameters.ArchivePath, ct),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            (post, token) =>
            {
                // Если стоит на паузе — блокируем выполнение
                _pauseEvent.Wait(token);

                _indexService.Add(post);

                int count = Interlocked.Increment(ref total);

                if (lastTime.Elapsed >= oneSecond)
                {
                    lastTime.Restart();
                    _messages.OnNext($"Импортировано {count:N0} документов");
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        _messages.OnNext($"Импортировано {total:N0} документов");

        return total;
    }

    private async Task<ImportResult> ImportAsync(ImportParameters parameters, CancellationToken ct)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            await _indexService.ClearAsync(ct).ConfigureAwait(false);

            var documentCount = await ImportDocumentsAsync(parameters, ct).ConfigureAwait(false);

            _messages.OnNext("Оптимизация индекса");
            await _indexService.OptimizeAsync(parameters.IndexOptimization, ct).ConfigureAwait(false);

            _messages.OnNext("Завершение импорта");
            await _indexService.CommitAsync(ct).ConfigureAwait(false);

            _indexService.Refresh();

            _messages.OnNext("Импорт завершен");

            return new ImportCompletedResult(parameters, documentCount, sw.Elapsed);
        }
        catch (Exception err)
        {
            await _indexService.RollbackAsync(default).ConfigureAwait(false);

            //if (err is OperationCanceledException)
            //{
            //    throw;
            //}

            return new ImportFailedResult(parameters, err);
        }
    }

    private readonly IArchiveReader _archiveReader;
    private readonly IIndexService _indexService;
    private readonly ObservableAsPropertyHelper<string> _messageProperty;
    private readonly ObservableAsPropertyHelper<TimeSpan> _elapsedProperty;
    private readonly Subject<string> _messages = new();
    private readonly ManualResetEventSlim _pauseEvent = new(initialState: true);

    private ImportParameters? _parameters;

    #endregion
}
