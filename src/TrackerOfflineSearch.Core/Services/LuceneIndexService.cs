using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Services;

public sealed class LuceneIndexService : IIndexService, IDisposable
{
    #region Constructor

    public LuceneIndexService(ILogger<LuceneIndexService> logger, IPostMapper mapper)
    {
        //_indexPath = @"E:\Temp\ShortIndex";
        _indexPath = @"E:\Temp\Index"; // read from settings

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        if (!System.IO.Directory.Exists(_indexPath))
        {
            System.IO.Directory.CreateDirectory(_indexPath);
        }

        _analyzer = new StandardAnalyzer(AppConsts.SearchEngineVersion);
        _directory = FSDirectory.Open(_indexPath);

        if (!DirectoryReader.IndexExists(_directory))
        {
            var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND,
            };
            using var writer = new IndexWriter(_directory, config);
            writer.Commit();
        }

        _reader = DirectoryReader.Open(_directory);

        _titleParser = new QueryParser(AppConsts.SearchEngineVersion, Post.TitleField, _analyzer);
        _contentParser = new QueryParser(AppConsts.SearchEngineVersion, Post.ContentField, _analyzer);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _reader?.Dispose();
        //_writer.Dispose();
        _directory.Dispose();
        _analyzer.Dispose();
    }

    #endregion

    #region IIndexService - search

    public int TotalCount => _reader.NumDocs;

    public IEnumerable<Forum> GetForums()
    {
        var forums = new List<Forum>();

        Fields fields = MultiFields.GetFields(_reader);
        if (fields is null)
        {
            return forums;
        }

        Terms terms = fields.GetTerms(Post.ForumNameField);
        if (terms is null)
            return forums;

        TermsEnum iterator = terms.GetEnumerator(null);
        if (iterator is null)
        {
            return forums;
        }

        while (iterator.MoveNext())
        {
            forums.Add(new(iterator.Term.Utf8ToString()));
        }

        return GetForumWithAncestors(forums);
    }

    public SearchResult Search(PostQuery postQuery, int limit = 300)
    {
        var searcher = new IndexSearcher(_reader);

        var query = BuildQuery(postQuery);

        var sort = new Sort(
            SortField.FIELD_SCORE,
            new SortField(Post.CreatedSortField, SortFieldType.INT64, true)
            );

        var topDocs = searcher.Search(query, limit, sort);

        var posts = topDocs.ScoreDocs
            .Select(sd => searcher.Doc(sd.Doc))
            .Select(_mapper.Map)
            .ToList();

        return new SearchResult(posts, (int)topDocs.TotalHits);
    }

    public IIndexWriterSession OpenWriterSession() => new IndexWriterSession(this);

    #endregion

    #region IIndexWriterSession

    private sealed class IndexWriterSession : IIndexWriterSession
    {
        public IndexWriterSession(LuceneIndexService indexService)
        {
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));

            var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, indexService._analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND,
                RAMBufferSizeMB = 512, // read from settings
            };
            _writer = new IndexWriter(indexService._directory, config);

            _pool = ObjectPool.Create<PostDocument>();
        }

        public void Dispose()
        {
            if (_hasChanges)
            {
                _writer.Rollback();
            }
            _writer.Dispose();
        }

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

        public Task CommitAsync(CancellationToken cancellation) => 
            Task.Run(() => 
                {
                    _writer.Commit();
                    _hasChanges = false;

                    _indexService.Refresh();
                }, 
                cancellation
            );

        public Task OptimizeAsync(IndexOptimizationStrategy strategy, CancellationToken cancellation) =>
            Task.Run(() => Optimize(strategy), cancellation);

        private void Optimize(IndexOptimizationStrategy strategy)
        {
            int maxSegments = strategy switch
            {
                IndexOptimizationStrategy.Minimum => 100,
                IndexOptimizationStrategy.Low => 20,
                IndexOptimizationStrategy.Normal => 10,
                IndexOptimizationStrategy.High => 5,
                IndexOptimizationStrategy.Maximum => 1,
                _ => throw new ArgumentException("Unsupported optimization strategy", nameof(strategy))
            };

            _writer.ForceMerge(maxSegments);
            _hasChanges = true;
        }

        private readonly LuceneIndexService _indexService;
        private readonly IndexWriter _writer;
        private readonly ObjectPool<PostDocument> _pool;

        private bool _hasChanges = false;
    }

    #endregion

    #region Private

    /// <summary>
    /// Возвращает коллекцию форумов, включая всех их предков (родительские форумы) до корневого уровня.
    /// Для отсутствующих в исходной коллекции предков создаются новые элементы.
    /// </summary>
    /// <param name="forums">Исходная коллекция форумов</param>
    /// <returns>Коллекция форумов, содержащая исходные форумы и всех их предков до корневого уровня</returns>
    private static IReadOnlyCollection<Forum> GetForumWithAncestors(IEnumerable<Forum> forums)
    {
        var result = forums.ToDictionary(x => x.Id, StringComparer.Ordinal);

        foreach (var forum in forums)
        {
            var parentId = forum.ParentId;
            while (!string.IsNullOrEmpty(parentId) && !result.ContainsKey(parentId))
            {
                var parent = new Forum(parentId);
                result[parentId] = parent;
                parentId = parent.ParentId;
            }
        }

        return result.Values;
    }

    private void Refresh()
    {
        var newReader = DirectoryReader.OpenIfChanged(_reader);
        if (newReader != null)
        {
            var oldReader = _reader;
            _reader = newReader;
            oldReader.Dispose();
        }
    }

    private Query BuildQuery(PostQuery postQuery)
    {
        if (postQuery.IsEmpty)
        {
            return new MatchAllDocsQuery();
        }

        var filters = new[] {
            GetTitleQuery(postQuery),
            GetContentQuery(postQuery),
            GetForumQuery(postQuery),
            GetDateQuery(postQuery),
            GetSizeQuery(postQuery)
        };

        var query = new BooleanQuery();
        foreach (var q in filters.Where(f => f is not null))
        {
            query.Add(q, Occur.MUST);
        }

        return query;
    }

    private Query? GetTitleQuery(PostQuery postQuery)
    {
        if (!postQuery.HasTitleQuery())
            return null;

        return _titleParser.Parse(postQuery.Title);
    }

    private Query? GetContentQuery(PostQuery postQuery)
    {
        if (!postQuery.HasContentQuery())
            return null;

        return _contentParser.Parse(postQuery.Content);
    }

    private static Query? GetForumQuery(PostQuery postQuery)
    {
        if (!postQuery.HasForumFilter())
            return null;

        return new BooleanQuery()
        {
            { new TermQuery(new Term(Post.ForumNameField, postQuery.Forum)), Occur.SHOULD },
            { new PrefixQuery(new Term(Post.ForumNameField, postQuery.Forum + Forum.Separator)), Occur.SHOULD },
        };
        //return new PrefixQuery(new Term(Post.ForumNameField, postQuery.Forum));
    }

    private static Query? GetDateQuery(PostQuery postQuery)
    {
        static string? DateToString(DateTime dt) => DateTools.DateToString(dt, AppConsts.DefaultDateResolution);

        static BytesRef? ToBytesRef(DateTime? dt) => dt.HasValue ? new BytesRef(DateToString(dt.Value)) : null;

        if (!postQuery.HasDateFilter())
            return null;

        var fromDate = ToBytesRef(postQuery.MinDate);
        var toDate = ToBytesRef(postQuery.MaxDate);

        return new TermRangeQuery(Post.CreatedField, fromDate, toDate, true, true);
    }

    private static Query? GetSizeQuery(PostQuery postQuery)
    {
        if (!postQuery.HasSizeFilter())
            return null;

        //return NumericRangeFilter.NewInt64Range(Post.CreatedField, postQuery.FromSizeFilter, postQuery.ToSizeFilter, true, true);
        return NumericRangeQuery.NewInt64Range(Post.SizeField, postQuery.MinSize, postQuery.MaxSize, true, true);
    }

    private readonly string _indexPath;
    private readonly ILogger<LuceneIndexService> _logger;
    private readonly IPostMapper _mapper;

    private readonly FSDirectory _directory;
    private readonly Analyzer _analyzer;
    private DirectoryReader _reader;

    private readonly QueryParser _titleParser;
    private readonly QueryParser _contentParser;

    #endregion
}
