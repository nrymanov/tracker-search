using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Services;

public sealed class LuceneSearchService : IIndexSearchService, IDisposable
{
    #region Constructor

    public LuceneSearchService(ILogger<LuceneSearchService> logger, IPostMapper mapper)
    {
        //_indexPath = @"E:\Temp\ShortIndex";
        _indexPath = @"E:\Temp\Index";

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        if (!System.IO.Directory.Exists(_indexPath))
        {
            System.IO.Directory.CreateDirectory(_indexPath);
        }

        _analyzer = new StandardAnalyzer(AppConsts.SearchEngineVersion);
        _directory = FSDirectory.Open(_indexPath);

        var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
        };

        using (var writer = new IndexWriter(_directory, config))
        {
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
        _directory?.Dispose();
        _analyzer?.Dispose();
    }

    #endregion

    #region IIndexSearchService

    public int TotalCount => _reader.NumDocs;

    public IEnumerable<Forum> GetForums()
    {
        var result = new List<Forum>();

        Fields fields = MultiFields.GetFields(_reader);
        if (fields is null)
        {
            return result;
        }

        Terms terms = fields.GetTerms(Post.ForumNameField);
        if (terms is null)
            return result;

        TermsEnum iterator = terms.GetEnumerator(null);
        if (iterator is null)
        {
            return result;
        }

        while (iterator.MoveNext())
        {
            result.Add(new(iterator.Term.Utf8ToString()));
        }

        return result;
    }

    public SearchResult Search(PostQuery postQuery, int limit = 100)
    {
        var searcher = new IndexSearcher(_reader);

        var query = BuildQuery(postQuery);

        var sort = new Sort(
            SortField.FIELD_SCORE,
            new SortField(Post.CreatedField, SortFieldType.STRING, true)
            );

        var topDocs = searcher.Search(query, limit, sort);

        var posts = topDocs.ScoreDocs
            .Select(sd => searcher.Doc(sd.Doc))
            .Select(_mapper.Map)
            .ToList();

        return new SearchResult(posts, (int)topDocs.TotalHits);
    }

    public void Refresh()
    {
        var newReader = DirectoryReader.OpenIfChanged(_reader);
        if (newReader != null)
        {
            _reader.Dispose();

            // Идея так себе, но пока оставлю.
            //
            // Что тут происходит:
            //
            // Lucene не может удалить файлы, пока существует хоть один DirectoryReader, который ссылается на старую версию индекса.
            //
            // Когда вызывается OpenIfChanged(), старый DirectoryReader закрывается и открывается новый.
            // Но важно: удаление файлов не происходит мгновенно — оно произойдёт только после того,
            // как IndexWriter будет открыт заново, и Lucene проведёт «deletion policy cleanup».
            // IndexWriter при создании выполняет проверку(IndexFileDeleter), которая:
            // - Сканирует индексную директорию.
            // - Удаляет все файлы, которые больше не используются ни одной актуальной версией индекса.
            // Это и есть тот момент, когда освобождается место.
            //
            // Пока IndexWriter жив и открыты DirectoryReader'ы на старые версии, файлы остаются, даже если они уже не нужны.
            //
            var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND,
            };
            using (var writer = new IndexWriter(_directory, config))
            {
                writer.Commit();
            }

            _reader = newReader;
        }
    }

    #endregion

    #region Private

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

        //return new PrefixFilter(new Term(Post.ForumNameField, postQuery.ForumFilter));
        return new PrefixQuery(new Term(Post.ForumNameField, postQuery.Forum));
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
    private readonly ILogger<LuceneSearchService> _logger;

    private readonly FSDirectory _directory;
    private readonly Analyzer _analyzer;
    private readonly IPostMapper _mapper;

    private DirectoryReader _reader;

    private readonly QueryParser _titleParser;
    private readonly QueryParser _contentParser;

    #endregion
}
