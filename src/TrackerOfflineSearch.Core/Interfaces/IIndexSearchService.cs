using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IIndexSearchService
{
    int TotalCount { get; }

    IEnumerable<Forum> GetForums();

    SearchResult Search(PostQuery postQuery, int limit = 100);

    void Refresh();
}
