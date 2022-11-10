using Lucene.Net.Search;

namespace TrackerOfflineSearch.Services;

public interface IQueryBuilder
{
    bool TryBuild(string queryString, out Query? query);

    Query Build(string queryString);
}
