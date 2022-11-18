using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Domain;
using TrackerOfflineSearch.Helpers;

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
        this._mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this._analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._indexPath = fs?.MainIndexPath ?? throw new ArgumentNullException(nameof(fs)); 

        this._searchSubject = new Subject<Query>();

        this._searchSubject
            .Select(q => Observable.FromAsync(ct => this.SearchPosts(q, ct)))
            //.SelectMany(SearchPosts)
            .Switch()
            .Subscribe(posts => 
                this._items.Edit(el => { 
                    el.Clear();
                    el.AddRange(posts);
                })
            );

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

    public void Search(Query query)
    {
        this._searchSubject.OnNext(query);
    }

    public IObservable<IChangeSet<Post>> Connect() => this._items.Connect();

    public IReadOnlyList<string> Forums 
    {
        get
        {
            return this.ReadIndex(r => {
                Fields fields = MultiFields.GetFields(r.Reader);
                Terms terms = fields.GetTerms(Post.ForumNameField);
                TermsEnum iterator = terms.GetEnumerator(null);

                var result = new List<string>();
                while (iterator.MoveNext())
                {
                    result.Add(iterator.Term.Utf8ToString());
                }
                return result;
            });
        }
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

        public IEnumerator<Document> Search(Query query)
        {
            var searcher = new IndexSearcher(this.Reader);

            var sort = new Sort(
                SortField.FIELD_SCORE,
                new SortField(Post.CreatedField, SortFieldType.STRING, true)
                );

            TopDocs topDocs = searcher.Search(query, 100, sort);
            ScoreDoc[] hits = topDocs.ScoreDocs;

            foreach (var h in hits)
            {
                yield return searcher.Doc(h.Doc);
            }
            //return hits.Select(hit => searcher.Doc(hit.Doc));
        }

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
            using var p = Profiler.Start(this._logger);
            _writer.DeleteAll();
        }

        public RAMDirectory CreateChunk(Post[] posts)
        {
            using var p = Profiler.Start(this._logger);

            var dir = new RAMDirectory();
            using var writer = new IndexWriter(dir, this._indexConfig);

            writer.AddDocuments(posts.Select(_mapper.ToRepository));

            return dir;
        }

        public int Add(RAMDirectory index)
        {
            using var p = Profiler.Start(this._logger);

            if (index is null)
                throw new ArgumentNullException(nameof(index));

            _writer.AddIndexes(new[] { index });

            return _writer.NumDocs;
        }

        public void Optimize()
        {
            using var p = Profiler.Start(this._logger);
            _writer.ForceMerge(1);
        }

        public void Commit()
        {
            using var p = Profiler.Start(this._logger);
            _writer.Commit();
        }

        public void Rollback()
        {
            using var p = Profiler.Start(this._logger);
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

    private Task<IEnumerable<Post>> SearchPosts(Query query, CancellationToken token)
    {
        this._logger.LogDebug("Search for {query} was started", query);

        return Task.Run(() => this.ReadIndex(r => {
            if (token.IsCancellationRequested)
            {
                this._logger.LogDebug("Search for {query} was cancelled", query);
                return Enumerable.Empty<Post>();
            }

            var searcher = new IndexSearcher(r.Reader);

            var sort = new Sort(
                SortField.FIELD_SCORE,
                new SortField(Post.CreatedField, SortFieldType.STRING, true)
                );

            TopDocs topDocs = searcher.Search(query, 100, sort);
            ScoreDoc[] hits = topDocs.ScoreDocs;

            if (token.IsCancellationRequested)
            {
                this._logger.LogDebug("Search for {query} was performed but result was discarded", query);
                return Enumerable.Empty<Post>();
            }

            this._logger.LogDebug("Search for {query} was completed", query);

            return hits.Select(hit => _mapper.ToDomain(searcher.Doc(hit.Doc))).ToList();
        }), token);
    }

    //protected IObservable<Post> _SearchPosts(Query query)
    //{
    //    var o = Observable.Using(
    //        () => new ReposytoryReader(_indexPath, _analyzer),
    //        r => Observable.Generate(
    //            r.Search(query),
    //            e => e.MoveNext(),
    //            e => e,
    //            e => _mapper.ToDomain(e.Current)
    //            )
    //        );

    //    return o;
    //}

    private readonly IPostMapper _mapper;
    private readonly Analyzer _analyzer;
    private readonly ILogger<PostRepository> _logger;
    private readonly string _indexPath;
    private readonly Subject<Query> _searchSubject;
    private readonly SourceList<Post> _items = new SourceList<Post>();

    #endregion
}