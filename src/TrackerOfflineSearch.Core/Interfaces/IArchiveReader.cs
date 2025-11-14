using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IArchiveReader
{
    IAsyncEnumerable<Post> ReadPostsAsync(
        string arhiveFilePath,
        bool skipContent,
        CancellationToken ct
    );
}
