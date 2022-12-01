using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services.Implementation;

public class QueryBuilder : IQueryBuilder
{
    #region Constructor

    public QueryBuilder(Analyzer analyzer, ILogger<QueryBuilder> logger)
    {
        this.analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.titleParser = new QueryParser(AppConst.SearchEngineVersion, Post.TitleField, this.analyzer);
        this.contentParser = new QueryParser(AppConst.SearchEngineVersion, Post.ContentField, this.analyzer);
    }

    #endregion

    #region IQueryBuilder implementation

    public bool TryBuild(PostQuery postQuery, out Query? searchParams)
    {
        try
        {
            searchParams = this.Build(postQuery);
            return true;
        }
        catch (ParseException e)
        {
            this.logger.LogError(e, "Error parsing query '{query}'", postQuery);

            searchParams = null;
            return false;
        }
    }

    public Query Build(PostQuery postQuery)
    {
        if (postQuery.IsEmpty)
            return new MatchAllDocsQuery();

        var filters = new[] {
            this.GetTitleQuery(postQuery),
            this.GetContentQuery(postQuery),
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

    #endregion

    #region Private fields & methods

    private Query? GetTitleQuery(PostQuery postQuery)
    {
        if (!postQuery.HasTitleQuery)
            return null;

        return this.titleParser.Parse(postQuery.TitleQuery);
    }

    private Query? GetContentQuery(PostQuery postQuery)
    {
        if (!postQuery.HasContentQuery)
            return null;

        return this.contentParser.Parse(postQuery.ContentQuery);
    }

    private static Query? GetForumQuery(PostQuery postQuery)
    {
        if (!postQuery.HasForumFilter)
            return null;

        //return new PrefixFilter(new Term(Post.ForumNameField, postQuery.ForumFilter));
        return new PrefixQuery(new Term(Post.ForumNameField, postQuery.ForumFilter));
    }

    private static Query? GetDateQuery(PostQuery postQuery)
    {
        if (!postQuery.HasDateFilter)
            return null;

        var (d1, d2) = postQuery.Interval.Dates;

        var fromDate = d1.ToBytesRef();
        var toDate = d2.ToBytesRef();

        return new TermRangeQuery(Post.CreatedField, fromDate, toDate, true, true);
    }

    private static Query? GetSizeQuery(PostQuery postQuery)
    {
        if (!postQuery.HasSizeFilter)
            return null;

        //return NumericRangeFilter.NewInt64Range(Post.CreatedField, postQuery.FromSizeFilter, postQuery.ToSizeFilter, true, true);
        return NumericRangeQuery.NewInt64Range(Post.SizeField, postQuery.FromSizeFilter, postQuery.ToSizeFilter, true, true);
    }
    
    private readonly Analyzer analyzer;
    private readonly ILogger<QueryBuilder> logger;
    private readonly QueryParser titleParser;
    private readonly QueryParser contentParser;

    #endregion
}
