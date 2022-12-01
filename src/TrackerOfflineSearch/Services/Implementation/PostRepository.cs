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
using Prism.Events;
using TrackerOfflineSearch.Domain;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Helpers;

namespace TrackerOfflineSearch.Services.Implementation;

public class PostRepository : IPostRepository
{
    #region Constructor

    public PostRepository(
        IEventAggregator eventAggregator,
        IPostMapper mapper, 
        IFileSystem fs, 
        Analyzer analyzer, 
        ILogger<PostRepository> logger
        )
    {
        this.eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.indexPath = fs?.MainIndexPath ?? throw new ArgumentNullException(nameof(fs)); 

        this.searchSubject = new Subject<Query>();

        this.searchSubject
            .Do(_ => this.eventAggregator.GetEvent<SearchActiveEvent>().Publish(true))
            .Select(q => Observable.FromAsync(ct => this.SearchPosts(q, ct)))
            .Switch()
            .Subscribe(posts =>
            {
                this.items.Edit(el =>
                {
                    el.Clear();
                    el.AddRange(posts);
                });

                this.eventAggregator.GetEvent<SearchActiveEvent>().Publish(false);
            });

        this.logger.LogDebug("Store index in \"{indexPath}\" folder", this.indexPath);

        using var ws = this.NewWriteSession();
        ws.Commit();
    }

    #endregion

    #region IPostRepository implementation

    public int TotalItems => this.ReadIndex(r => r.Reader.NumDocs);

    public void Search(Query query) => this.searchSubject.OnNext(query);

    public IObservable<IChangeSet<Post>> Connect() => this.items.Connect();

    public IReadOnlyList<string> Forums 
    {
        get 
        {
            return 
                this.ReadIndex(r => {
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

    public IWriteSession NewWriteSession() => new WriteSession(this.indexPath, this.analyzer, this.mapper, this.logger);

    #endregion

    #region Internal classes

    private class ReposytoryReader : IDisposable
    {
        public ReposytoryReader(string indexPath, Analyzer analyzer)
        {
            this.IndexPath = indexPath;
            this.Analyzer = analyzer;

            this.Directory = FSDirectory.Open(this.IndexPath);
            this.Reader = DirectoryReader.Open(this.Directory);
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
            this.Reader.Dispose();
            this.Directory.Dispose();
        }
    }

    private class WriteSession : IWriteSession
    {
        public WriteSession(string indexPath, Analyzer analyzer, IPostMapper mapper, ILogger<PostRepository> logger)
        {
            this._indexPath = indexPath;
            this._analyzer = analyzer;
            this._mapper = mapper;
            this._logger = logger;

            this._indexConfig = new IndexWriterConfig(AppConst.SearchEngineVersion, this._analyzer) 
            { 
                OpenMode = OpenMode.CREATE_OR_APPEND,
                //RAMBufferSizeMB = 1024,
            };

            this._directory = FSDirectory.Open(this._indexPath);
            this._writer = new IndexWriter(this._directory, this._indexConfig);
        }

        public void DeleteAll()
        {
            using var p = Profiler.Start(this._logger);
            this._writer.DeleteAll();
        }

        public RAMDirectory CreateChunk(Post[] posts)
        {
            using var p = Profiler.Start(this._logger);

            var dir = new RAMDirectory();
            using var writer = new IndexWriter(dir, this._indexConfig);

            writer.AddDocuments(posts.Select(this._mapper.ToRepository));

            return dir;
        }

        public int Add(RAMDirectory index)
        {
            using var p = Profiler.Start(this._logger);

            if (index is null)
                throw new ArgumentNullException(nameof(index));

            this._writer.AddIndexes(new[] { index });

            return this._writer.NumDocs;
        }

        public void Optimize()
        {
            using var p = Profiler.Start(this._logger);
            this._writer.ForceMerge(1);
        }

        public void Commit()
        {
            using var p = Profiler.Start(this._logger);
            this._writer.Commit();
        }

        public void Rollback()
        {
            using var p = Profiler.Start(this._logger);
            this._writer.Rollback();
        }

        public void Dispose()
        {
            this._writer.Dispose();
            this._directory.Dispose();
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

        using var r = new ReposytoryReader(this.indexPath, this.analyzer);

        return readFunc(r);
    }

    private Task<IEnumerable<Post>> SearchPosts(Query query, CancellationToken token)
    {
        this.logger.LogDebug("Search for {query} was started", query);

        return Task.Run(() => this.ReadIndex(r => {
            if (token.IsCancellationRequested)
            {
                this.logger.LogDebug("Search for {query} was cancelled", query);
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
                this.logger.LogDebug("Search for {query} was performed but result was discarded", query);
                return Enumerable.Empty<Post>();
            }

            this.logger.LogDebug("Search for {query} was completed", query);

            return hits.Select(hit => this.mapper.ToDomain(searcher.Doc(hit.Doc))).ToList();
        }), token);
    }

    private readonly IEventAggregator eventAggregator;
    private readonly IPostMapper mapper;
    private readonly Analyzer analyzer;
    private readonly ILogger<PostRepository> logger;
    private readonly string indexPath;
    private readonly Subject<Query> searchSubject;
    private readonly SourceList<Post> items = new();

    #endregion
}
