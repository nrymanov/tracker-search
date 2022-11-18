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
    private readonly Analyzer _analyzer;
    private readonly ILogger<QueryBuilder> _logger;
    private readonly QueryParser _titleParser;
    private readonly QueryParser _contentParser;

    public QueryBuilder(Analyzer analyzer, ILogger<QueryBuilder> logger)
    {
        this._analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._titleParser = new QueryParser(AppConst.SearchEngineVersion, Post.TitleField, this._analyzer);
        this._contentParser = new QueryParser(AppConst.SearchEngineVersion, Post.ContentField, this._analyzer);
    }

    public Query Build(PostQuery postQuery)
    {
        if (postQuery.IsEmpty)
            return new MatchAllDocsQuery();

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

    public bool TryBuild(PostQuery postQuery, out Query? searchParams)
    {
        try
        {
            searchParams = this.Build(postQuery);
            return true;
        }
        catch (ParseException e)
        {
            this._logger.LogError(e, "Error parsing query '{query}'", postQuery);

            searchParams = null;
            return false;
        }
    }

    private Query? GetTitleQuery(PostQuery postQuery)
    {
        if (!postQuery.HasTitleQuery)
            return null;

        return this._titleParser.Parse(postQuery.TitleQuery);
    }

    private Query? GetContentQuery(PostQuery postQuery)
    {
        if (!postQuery.HasContentQuery)
            return null;

        return this._contentParser.Parse(postQuery.ContentQuery);
    }

    private Query? GetForumQuery(PostQuery postQuery)
    {
        if (!postQuery.HasForumFilter)
            return null;

        //return new PrefixFilter(new Term(Post.ForumNameField, postQuery.ForumFilter));
        return new PrefixQuery(new Term(Post.ForumNameField, postQuery.ForumFilter));
    }

    private Query? GetDateQuery(PostQuery postQuery)
    {
        if (!postQuery.HasDateFilter)
            return null;

        // TODO round Dates 
        var fromDate = postQuery.FromDateFilter.ToBytesRef();
        var toDate = postQuery.ToDateFilter.ToBytesRef();

        //return new TermRangeFilter(Post.CreatedField, fromDate, toDate, true, true);
        return new TermRangeQuery(Post.CreatedField, fromDate, toDate, true, true);
    }

    private Query? GetSizeQuery(PostQuery postQuery)
    {
        if (!postQuery.HasSizeFilter)
            return null;

        //return NumericRangeFilter.NewInt64Range(Post.CreatedField, postQuery.FromSizeFilter, postQuery.ToSizeFilter, true, true);
        return NumericRangeQuery.NewInt64Range(Post.SizeField, postQuery.FromSizeFilter, postQuery.ToSizeFilter, true, true);
    }
}
