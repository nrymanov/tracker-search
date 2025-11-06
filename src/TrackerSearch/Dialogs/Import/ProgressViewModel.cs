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

        _elapsedProperty = ImportCommand.IsExecuting
            .SelectMany(isRunning =>
                isRunning
                    ? Observable.Interval(TimeSpan.FromSeconds(1)).StartWith(0).Select(i => TimeSpan.FromSeconds(i))
                    : Observable.Return(TimeSpan.Zero)
            )
            .ObserveOn(RxApp.MainThreadScheduler)
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

    private readonly IArchiveReader _archiveReader;
    private readonly IIndexService _indexService;
    private readonly ObservableAsPropertyHelper<string> _messageProperty;
    private readonly ObservableAsPropertyHelper<TimeSpan> _elapsedProperty;
    private readonly Subject<string> _progressMessageSubject = new();
    private readonly ManualResetEventSlim _pauseEvent = new(initialState: true);

    private ImportParameters? _parameters;

    #endregion
}
