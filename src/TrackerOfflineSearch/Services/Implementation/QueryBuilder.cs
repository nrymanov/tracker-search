using System;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services.Implementation;

public class QueryBuilder : IQueryBuilder
{
    private readonly Analyzer _analyzer;
    private readonly ILogger<QueryBuilder> _logger;
    private readonly QueryParser _parser;

    public QueryBuilder(Analyzer analyzer, ILogger<QueryBuilder> logger)
    {
        this._analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._parser = new QueryParser(AppConst.SearchEngineVersion, Post.TitleField, this._analyzer);
    }

    public Query Build(string queryString)
    {
        return string.IsNullOrEmpty(queryString) 
            ? new MatchAllDocsQuery() 
            : this._parser.Parse(queryString);
    }

    public bool TryBuild(string queryString, out Query? query)
    {
        try
        {
            query = this.Build(queryString);
            return true;
        }
        catch (ParseException e)
        {
            this._logger.LogError(e, "Error parsing query '{query}'", queryString);
            
            query = null;
            return false;
        }
    }
}
