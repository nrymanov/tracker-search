using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services;

public interface IIndexService
{
    int TotalCount { get; }

    IEnumerable<Forum> GetForums();

    SearchResult Search(PostQuery postQuery, int limit = 100);

    IIndexWriterSession OpenWriterSession();
}
