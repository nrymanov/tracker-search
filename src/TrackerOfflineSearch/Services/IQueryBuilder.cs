using Lucene.Net.Search;

namespace TrackerOfflineSearch.Services;

public interface IQueryBuilder
{
    bool TryBuild(PostQuery postQuery, out Query? searchParams);

    Query Build(PostQuery postQuery);
}
