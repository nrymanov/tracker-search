using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ReactiveUI;
using TrackerOfflineSearch.Core.Models;
using TrackerOfflineSearch.Settings;

namespace TrackerOfflineSearch.Services.Implementation;

public class ImportManager : ReactiveObject, IImportManager
{
    #region Constructor

    public ImportManager(IArchiveManager archiveManager, Func<IPostRepositoryWriter> writerFactory, IAppSettings settings)
    {
        this.archiveManager = archiveManager ?? throw new ArgumentNullException(nameof(archiveManager));
        this.writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        this.cts = new CancellationTokenSource();
    }

    #endregion

    #region Public properties & methods

    public int ImportCount
    {
        get => this.importTotal;
        set => this.RaiseAndSetIfChanged(ref this.importTotal, value);
    }

    public async Task ImportAsync(string archivePath)
    {
        var ct = this.cts.Token;

        using var writer = this.writerFactory();

        var cleanerBlock = new TransformBlock<string, string>(
            path =>
            {
                ct.ThrowIfCancellationRequested();

                writer.DeleteAll();
                return path;
            },
            new ExecutionDataflowBlockOptions { BoundedCapacity = 2, CancellationToken = ct }
            );

        var readerBlock = this.CreateReaderBlock(ct);

        var writerBlock = new TransformBlock<Post[], int>(
            posts => 
            { 
                ct.ThrowIfCancellationRequested();

                return writer.Add(posts);
            },
            new ExecutionDataflowBlockOptions { BoundedCapacity = 2, CancellationToken = ct }
            );

        var updateProgressBlock = new ActionBlock<int>(
            count => this.ImportCount = count,
            new ExecutionDataflowBlockOptions { CancellationToken = ct }
        );

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        cleanerBlock.LinkTo(readerBlock, linkOptions);
        readerBlock.LinkTo(writerBlock, linkOptions);
        writerBlock.LinkTo(updateProgressBlock, linkOptions);

        //
        // Start processing
        //
        try
        {
            ct.ThrowIfCancellationRequested();

            await cleanerBlock.SendAsync(archivePath, ct);
            cleanerBlock.Complete();

            await Task.WhenAll(
                cleanerBlock.Completion,
                readerBlock.Completion,
                writerBlock.Completion,
                updateProgressBlock.Completion
                );

            await Task.Run(() => writer.Commit(), ct);
        }
        catch (OperationCanceledException)
        {
            await Task.Run(() => writer.Rollback());
            throw;
        }
    }

    public async Task OptimizeAsync()
    {
        var ct = this.cts.Token;
        using var writer = this.writerFactory();
        try
        {
            ct.ThrowIfCancellationRequested();

            await Task.Run(() => writer.Optimize(), ct);
        }
        catch (OperationCanceledException)
        {
            await Task.Run(() => writer.Rollback());
            throw;
        }
    }

    public void Cancel()
    {
        this.cts.Cancel();
    }

    #endregion

    #region Private properties & methods

    private IPropagatorBlock<string, Post[]> CreateReaderBlock(CancellationToken ct)
    {
        var outBlock = new BufferBlock<Post[]>(new DataflowBlockOptions { BoundedCapacity = 2, CancellationToken = ct });

        var inBlock = new ActionBlock<string>(
            async path =>
            {
                foreach (var t in this.archiveManager.GetPosts(path).Chunk(this.settings.ChunkSize))
                {
                    ct.ThrowIfCancellationRequested();

                    await outBlock.SendAsync(t, ct);
                }
            },
            new ExecutionDataflowBlockOptions { CancellationToken = ct }
        );
        inBlock.Completion.ContinueWith(task =>
        {
            if (task.IsFaulted)
                ((IDataflowBlock)outBlock).Fault(task.Exception);
            else
                outBlock.Complete();
        }, ct);

        return DataflowBlock.Encapsulate(inBlock, outBlock);
    }

    private readonly IArchiveManager archiveManager;
    private readonly Func<IPostRepositoryWriter> writerFactory;
    private readonly IAppSettings settings;
    private readonly CancellationTokenSource cts;
    private int importTotal;

    #endregion
}
