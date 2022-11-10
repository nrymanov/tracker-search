using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Search;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services;

public interface IPostRepository
{
    int TotalItems { get; }

    IEnumerable<Post> Search(Query query, CancellationToken token);

    IWriteSession NewWriteSession();
}
