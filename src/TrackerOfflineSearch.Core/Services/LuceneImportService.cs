using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Services;

public sealed class LuceneImportService : IIndexImportService, IDisposable
{
    private readonly ILogger<LuceneImportService> _logger;
    private readonly string _indexPath;
    private readonly FSDirectory _directory;
    private readonly Analyzer _analyzer;
    private readonly IndexWriter _writer;

    private readonly ObjectPool<PostDocument> _pool;

    public LuceneImportService(ILogger<LuceneImportService> logger)
    {
        //_indexPath = @"E:\Temp\ShortIndex";
        _indexPath = @"E:\Temp\Index";

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!System.IO.Directory.Exists(_indexPath))
        {
            System.IO.Directory.CreateDirectory(_indexPath);
        }

        _analyzer = new StandardAnalyzer(AppConsts.SearchEngineVersion);
        _directory = FSDirectory.Open(_indexPath);

        var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
            //RAMBufferSizeMB = 512,
            RAMBufferSizeMB = 64,
            UseCompoundFile = false,
            MergeScheduler = new ConcurrentMergeScheduler(),
            // MaxBufferedDocs = -1, // отключить лимит по количеству документов
            //MergeScheduler = new SerialMergeScheduler(), // или ограничить фоновые слияния
            //MergePolicy = NoMergePolicy.NO_COMPOUND_FILES, // запрещает объединения
        };

        _writer = new IndexWriter(_directory, config);

        _pool = ObjectPool.Create<PostDocument>();
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _directory?.Dispose();
        _analyzer?.Dispose();
    }

    public void Add(Post post)
    {
        var doc = _pool.Get();
        try
        {
            //Lucene.Net.Documents.Document doc = _mapper.Map(post);
            _writer.AddDocument(doc.UpdateFrom(post));
        }
        finally
        {
            _pool.Return(doc);
        }
    }

    public void Clear() => _writer.DeleteAll();

    public void Optimize() => _writer.ForceMerge(1);

    public void Commit() => _writer.Commit();

    public void Rollback() => _writer.Rollback();
}
