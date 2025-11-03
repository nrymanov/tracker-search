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

        _messagePropery = _messages
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.Message);

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
    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

    #region Public

    public ProgressViewModel WithParameters(ImportParameters parameters)
    {
        _parameters = parameters;
        return this;
    }

    public ReactiveCommand<ImportParameters, ImportResult> ImportCommand { get; }

    public string Message => _messagePropery.Value;

    #endregion

    #region Private

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

            return new ImportResult(parameters, documentCount, sw.Elapsed);
        }
        catch
        {
            //
            // Доделать обработку ошибок от Writer-ов
            //
            await _indexService.RollbackAsync(ct).ConfigureAwait(false);

            throw;
        }
    }

    private readonly IArchiveReader _archiveReader;
    private readonly IIndexService _indexService;
    private readonly ObservableAsPropertyHelper<string> _messagePropery;
    private readonly Subject<string> _messages = new();

    private ImportParameters? _parameters;

    #endregion
}
