using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading.Channels;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;
using TrackerSearch.ViewModels;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace TrackerSearch.Dialogs.Import;

public class ProgressViewModel : ActivatableViewModel, IWizardPageViewModel
{
    public ProgressViewModel(
        IScreen screen,
        IArchiveReader archiveReader,
        IIndexImportService importService
        )
    {
        HostScreen = screen ?? throw new ArgumentNullException(nameof(screen));
        _archiveReader = archiveReader ?? throw new ArgumentNullException(nameof(archiveReader));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));

        ImportCommand = ReactiveCommand.CreateFromTask<ImportParameters, ImportResult>(ImportAsync);
        CancelCommand = ReactiveCommand.Create(() => { });

        _isBusyPropery = ImportCommand.IsExecuting.ToProperty(this, x => x.IsBusy);

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

    public string? UrlPathSegment => "import-progress";

    public IScreen HostScreen { get; }

    #region IWizardPageViewModel

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    // Ask confirmation
    public Task<bool> ConfirmCancelAsync() => Task.FromResult(true);

    #endregion

    public ProgressViewModel WithParameters(ImportParameters parameters)
    {
        _parameters = parameters;
        return this;
    }

    public ReactiveCommand<ImportParameters, ImportResult> ImportCommand { get; }

    public string Message => _messagePropery.Value;

    public bool IsBusy => _isBusyPropery.Value;

    private async Task<int> ImportDocumentsAsync(ImportParameters parameters, CancellationToken ct)
    {
        var startTime = DateTimeOffset.Now;

        var totalItems = new Subject<int>();

        using var messagesSubscription = totalItems.Scan(0, (acc, x) => acc + x)
            .Timestamp()
            .Select(x => (ts: TimeSpan.FromSeconds((int)(x.Timestamp - startTime).TotalSeconds), v: x.Value))
            .Select(x => $"Импортированно {x.v:N0} документов за {x.ts:g}")
            .Subscribe(_messages);

        var channel = Channel.CreateBounded<Post>(new BoundedChannelOptions(100) { SingleReader = false, SingleWriter = true, });

        var reader = channel.Reader;
        var consumers = Enumerable.Range(0, Math.Max(1, Environment.ProcessorCount / 2))
            .Select(_ => WritePostsToIndex(reader, totalItems, ct))
            .ToArray();

        ChannelWriter<Post> writer = channel.Writer;

        int documentsRead = 0;

        await foreach (var item in _archiveReader.ReadPostsAsync(parameters.ArchivePath, ct).ConfigureAwait(false))
        {
            if (item.IsNull)
            {
                continue;
            }

            await writer.WriteAsync(item, ct).ConfigureAwait(false);

            ++documentsRead;

            if (documentsRead > 30_000)
                break;
        }

        writer.Complete();

        var result = await Task.WhenAll(consumers).ConfigureAwait(false);

        var documentsWritten = result.Sum();

        if (documentsRead != documentsWritten)
        {
            Debug.Assert(documentsRead == documentsWritten);
            // Не все документы были записаны
        }

        return documentsWritten;
    }

    private async Task<ImportResult> ImportAsync(ImportParameters parameters, CancellationToken ct)
    {
        try
        {
            await _importService.ClearAsync(ct).ConfigureAwait(false);

            var documentCount = await ImportDocumentsAsync(parameters, ct).ConfigureAwait(false);

            _messages.OnNext("Оптимизация индекса");
            await _importService.OptimizeAsync(parameters.IndexOptimization, ct).ConfigureAwait(false);

            _messages.OnNext("Завершение импорта");
            await _importService.CommitAsync(ct).ConfigureAwait(false);

            _messages.OnNext("Импорт завершен");

            return new ImportResult(parameters, documentCount);
        }
        catch
        {
            //
            // Доделать обработку ошибок от Writer-ов
            //
            await _importService.RollbackAsync(ct).ConfigureAwait(false);

            throw;
        }
    }

    private async Task<int> WritePostsToIndex(ChannelReader<Post> reader, Subject<int> totalItems, CancellationToken ct)
    {
        int count = 0;
        int total = 0;

        await foreach (var post in reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            _importService.Add(post);

            ++total;
            if (++count == 1_000)
            {
                totalItems.OnNext(count);
                count = 0;
            }
        }

        totalItems.OnNext(count);

        return total;
    }

    private readonly IArchiveReader _archiveReader;
    private readonly IIndexImportService _importService;
    private readonly ObservableAsPropertyHelper<bool> _isBusyPropery;
    private readonly ObservableAsPropertyHelper<string> _messagePropery;
    private readonly Subject<string> _messages = new();

    private ImportParameters? _parameters;
}
