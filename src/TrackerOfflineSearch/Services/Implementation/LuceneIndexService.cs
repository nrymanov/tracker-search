using System.Globalization;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services.Implementation;

public sealed class LuceneIndexService : IIndexService, IDisposable
{
    #region Constructor

    public LuceneIndexService(
        ILogger<LuceneIndexService> logger,
        Analyzer analyzer,
        Directory directory
        )
    {
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(directory);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _reader = DirectoryReader.Open(directory);

        _titleParser = new QueryParser(AppConsts.SearchEngineVersion, Post.TitleField, analyzer);
        _contentParser = new QueryParser(AppConsts.SearchEngineVersion, Post.ContentField, analyzer);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _reader.Dispose();
    }

    #endregion

    #region IIndexService

    public int TotalCount => _reader.NumDocs;

    public IEnumerable<Forum> GetForums()
    {
        var forums = new List<Forum> { Forum.AllForums };

        var terms = MultiFields.GetTerms(_reader, Post.ForumNameField);
        if (terms is null)
        {
            return forums;
        }

        var iterator = terms.GetEnumerator();
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

    public SearchResult Search(PostQuery postQuery, int limit = 100)
    {
        var searcher = new IndexSearcher(_reader);

        var query = BuildQuery(postQuery);

        var sort = new Sort(
            SortField.FIELD_SCORE,
            new SortField(Post.CreatedSortField, SortFieldType.INT64, reverse: true)
            );

        _logger.LogDebug("Executing query {query}", query);

        var topDocs = searcher.Search(query, limit, sort);

        var posts = topDocs.ScoreDocs
            .Select(sd => searcher.Doc(sd.Doc))
            .Select(MapToPost)
            .ToList();

        return new SearchResult(posts, topDocs.TotalHits);
    }

    public void Refresh()
    {
        var newReader = DirectoryReader.OpenIfChanged(_reader);
        if (newReader != null)
        {
            var oldReader = _reader;
            _reader = newReader;
            oldReader.Dispose();
        }
    }

    #endregion

    #region Private

    private static Post MapToPost(Document doc, int index)
    {
        ArgumentNullException.ThrowIfNull(doc);

        return new Post
        {
            Id = doc.GetField(Post.IdField).GetInt32ValueOrDefault(),

            Created = new DateTime(doc.GetField(Post.CreatedField).GetInt64ValueOrDefault()),
            Size = doc.GetField(Post.SizeField).GetInt64ValueOrDefault(),

            Title = doc.Get(Post.TitleField, CultureInfo.InvariantCulture),
            Content = doc.Get(Post.ContentField, CultureInfo.InvariantCulture),

            Hash = doc.Get(Post.HashField, CultureInfo.InvariantCulture),

            TrackerId = doc.GetField(Post.TrackerIdField).GetInt32ValueOrDefault(),

            ForumId = doc.GetField(Post.ForumIdField).GetInt32ValueOrDefault(),
            ForumName = doc.Get(Post.ForumNameField, CultureInfo.InvariantCulture),

            Index = index,
        };
    }

    /// <summary>
    /// Возвращает коллекцию форумов, включая всех их предков (родительские форумы) до корневого уровня.
    /// Для отсутствующих в исходной коллекции предков создаются новые элементы.
    /// </summary>
    /// <param name="forums">Исходная коллекция форумов</param>
    /// <returns>Коллекция форумов, содержащая исходные форумы и всех их предков до корневого уровня</returns>
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = """
        Concrete types would not offer significant performance benefits in this context.
        """)]
    private static IEnumerable<Forum> GetForumWithAncestors(IEnumerable<Forum> forums)
    {
        var result = forums.ToDictionary(x => x.Id, StringComparer.Ordinal);

        foreach (var forum in forums)
        {
            if (forum.IsRoot)
            {
                continue;
            }

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
            GetSizeQuery(postQuery),
        };

        var query = new BooleanQuery();
        foreach (var q in filters.Where(f => f is not null))
        {
            query.Add(q, Occur.SHOULD);
        }

        return query;
    }

#pragma warning disable CA1859

    private Query? GetTitleQuery(PostQuery postQuery)
    {
        if (!postQuery.HasTitleQuery())
        {
            return null;
        }

        return _titleParser.Parse(postQuery.Title);
    }

    private Query? GetContentQuery(PostQuery postQuery)
    {
        if (!postQuery.HasContentQuery())
        {
            return null;
        }

        return _contentParser.Parse(postQuery.Content);
    }

    private static Query? GetForumQuery(PostQuery postQuery)
    {
        if (!postQuery.HasForumFilter())
        {
            return null;
        }

        return new BooleanQuery()
        {
            { new TermQuery(new Term(Post.ForumNameField, postQuery.Forum!.Path)), Occur.SHOULD },
            { new PrefixQuery(new Term(Post.ForumNameField, postQuery.Forum.SubForumPath)), Occur.SHOULD },
        };
        //return new PrefixQuery(new Term(Post.ForumNameField, postQuery.Forum));
    }

    private static Query? GetDateQuery(PostQuery postQuery)
    {
        static string? DateToString(DateTime dt) => DateTools.DateToString(dt, AppConsts.DefaultDateResolution);

        static BytesRef? ToBytesRef(DateTime? dt) => dt.HasValue ? new BytesRef(DateToString(dt.Value)) : null;

        if (!postQuery.HasDateFilter())
        {
            return null;
        }

        var fromDate = ToBytesRef(postQuery.MinDate);
        var toDate = ToBytesRef(postQuery.MaxDate);

        return new TermRangeQuery(Post.CreatedField, fromDate, toDate, includeLower: true, includeUpper: true);
    }

    private static Query? GetSizeQuery(PostQuery postQuery)
    {
        if (!postQuery.HasSizeFilter())
        {
            return null;
        }

        //return NumericRangeFilter.NewInt64Range(Post.CreatedField, postQuery.FromSizeFilter, postQuery.ToSizeFilter, true, true);
        return NumericRangeQuery.NewInt64Range(Post.SizeField, postQuery.MinSize, postQuery.MaxSize, minInclusive: true, maxInclusive: true);
    }

#pragma warning restore CA1859

    private readonly ILogger<LuceneIndexService> _logger;

    private DirectoryReader _reader;

    private readonly QueryParser _titleParser;
    private readonly QueryParser _contentParser;

    #endregion
}

