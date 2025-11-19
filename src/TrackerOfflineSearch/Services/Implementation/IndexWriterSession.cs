using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services.Implementation;

public sealed class IndexWriterSession : IIndexWriterSession
{
    #region Constructor

    public IndexWriterSession(
        ILogger<IndexWriterSession> logger,
        IOptions<ApplicationsOptions> options,
        Analyzer analyzer,
        Directory directory
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(directory);

        var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
            RAMBufferSizeMB = options.Value.RAMBufferSizeMB,
        };
        _writer = new IndexWriter(directory, config);

        _pool = ObjectPool.Create<PostDocument>();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_hasChanges)
        {
            _writer.Rollback();
        }
        _writer.Dispose();
    }

    #endregion

    #region IIndexWriterSession

    public Task ClearAsync(CancellationToken cancellation) =>
        Task.Run(() =>
            {
                _writer.DeleteAll();
                _hasChanges = true;
            },
            cancellation
        );

    public void Add(Post post)
    {
        var doc = _pool.Get();
        try
        {
            _writer.AddDocument(doc.UpdateFrom(post));
            _hasChanges = true;
        }
        finally
        {
            _pool.Return(doc);
        }
    }

    public Task OptimizeAsync(IndexOptimizationStrategy strategy, CancellationToken cancellation) =>
        Task.Run(() => OptimizeIndex(strategy), cancellation);

    public Task CommitAsync(CancellationToken cancellation) =>
        Task.Run(() =>
            {
                _writer.Commit();
                _hasChanges = false;
            },
            cancellation
        );

    public bool HasChanges => _hasChanges;

    #endregion

    #region Private

    private void OptimizeIndex(IndexOptimizationStrategy strategy)
    {
        var maxSegments = strategy switch
        {
            IndexOptimizationStrategy.Minimum => 100,
            IndexOptimizationStrategy.Low => 20,
            IndexOptimizationStrategy.Normal => 10,
            IndexOptimizationStrategy.High => 5,
            IndexOptimizationStrategy.Maximum => 1,
            _ => throw new ArgumentException("Unsupported optimization strategy", nameof(strategy)),
        };

        _writer.ForceMerge(maxSegments);
        _hasChanges = true;
    }

    private readonly IndexWriter _writer;
    private readonly ObjectPool<PostDocument> _pool;

    private bool _hasChanges;

    #endregion
}

