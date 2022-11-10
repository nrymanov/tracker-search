using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services.Implementation;

public class PostRepository : IPostRepository
{
    #region Constructor

    public PostRepository(
        IPostMapper mapper, 
        IFileSystem fs, 
        Analyzer analyzer, 
        ILogger<PostRepository> logger
        )
    {
        if (fs is null) throw new ArgumentNullException(nameof(fs));

        this._mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this._analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this._indexPath = Path.Combine(fs.AppDataDirectory, AppConst.IndexName);

        this._logger.LogDebug("Store index in \"{indexPath}\" folder", this._indexPath);

        using var ws = this.NewWriteSession();
        ws.Commit();
    }

    #endregion

    #region IPostRepository implementation

    public int TotalItems 
    { 
        get => ReadIndex(r => r.Reader.NumDocs);
    }

    public IEnumerable<Post> Search(Query query, CancellationToken token)
    {
        throw new System.NotImplementedException();
    }

    public IWriteSession NewWriteSession()
    {
        return new WriteSession(_indexPath, _analyzer, _mapper, _logger);
    }

    #endregion

    #region Internal classes

    private class ReposytoryReader : IDisposable
    {
        public ReposytoryReader(string indexPath, Analyzer analyzer)
        {
            IndexPath = indexPath;
            Analyzer = analyzer;

            Directory = FSDirectory.Open(IndexPath);
            Reader = DirectoryReader.Open(Directory);
        }

        public string IndexPath { get; }
        public Analyzer Analyzer { get; }
        public FSDirectory Directory { get; }
        public DirectoryReader Reader { get; }

        public void Dispose()
        {
            Reader.Dispose();
            Directory.Dispose();
        }
    }

    private class WriteSession : IWriteSession
    {
        public WriteSession(string indexPath, Analyzer analyzer, IPostMapper mapper, ILogger<PostRepository> logger)
        {
            _indexPath = indexPath;
            _analyzer = analyzer;
            _mapper = mapper;
            _logger = logger;

            this._indexConfig = new IndexWriterConfig(AppConst.SearchEngineVersion, _analyzer) 
            { 
                OpenMode = OpenMode.CREATE_OR_APPEND,
                //RAMBufferSizeMB = 1024,
            };

            _directory = FSDirectory.Open(_indexPath);
            _writer = new IndexWriter(_directory, this._indexConfig);
        }

        public void DeleteAll()
        {
            _writer.DeleteAll();
        }

        public RAMDirectory CreateChunk(Post[] posts)
        {
            var dir = new RAMDirectory();
            using var writer = new IndexWriter(dir, this._indexConfig);

            writer.AddDocuments(posts.Select(_mapper.ToRepository));

            return dir;
        }

        public int Add(RAMDirectory index)
        {
            if (index is null)
                throw new ArgumentNullException(nameof(index));

            _writer.AddIndexes(new[] { index });

            return _writer.NumDocs;
        }

        public void Commit()
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("Commit started");

            //_writer.ForceMerge(5);

            _writer.Commit();

            _logger.LogDebug("Commit finished in {time}", sw.Elapsed);
        }

        public void Rollback()
        {
            _writer.Rollback();
        }

        public void Dispose()
        {
            _writer.Dispose();
            _directory.Dispose();
        }

        private readonly string _indexPath;
        private readonly Analyzer _analyzer;
        private readonly IPostMapper _mapper;
        private readonly ILogger<PostRepository> _logger;
        private readonly IndexWriterConfig _indexConfig;
        private readonly FSDirectory _directory;
        private readonly IndexWriter _writer;
    }

    #endregion

    #region Private fields & methods

    private T ReadIndex<T>(Func<ReposytoryReader, T> readFunc)
    {
        System.Diagnostics.Debug.Assert(readFunc != null);

        using var r = new ReposytoryReader(_indexPath, _analyzer);

        return readFunc(r);
    }

    private readonly IPostMapper _mapper;
    private readonly Analyzer _analyzer;
    private readonly ILogger<PostRepository> _logger;
    private readonly string _indexPath;

    #endregion
}