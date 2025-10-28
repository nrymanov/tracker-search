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
using TrackerOfflineSearch.Core.Models;
using TrackerOfflineSearch.Events;

namespace TrackerOfflineSearch.Services.Implementation;

public class PostRepository : IPostRepository
{
    #region Constructor

    public PostRepository(
        Func<IPostRepositoryWriter> writerFactory,
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

        // TODO 00 initialize repository on app startup
        using var ws = writerFactory();
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
            return this.ReadIndex(r =>
            {
                var result = new List<string>();
                Fields fields = MultiFields.GetFields(r.Reader);
                if (fields is null)
                    return result;

                Terms terms = fields.GetTerms(Post.ForumNameField);
                if (terms is null)
                    return result;

                TermsEnum iterator = terms.GetEnumerator(null);
                if (iterator is null)
                    return result;

                while (iterator.MoveNext())
                {
                    result.Add(iterator.Term.Utf8ToString());
                }
                return result;
            });
        }
    }

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

            TopDocs topDocs = searcher.Search(query, 500, sort);
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
