using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services;

public interface IArchiveReader
{
    IAsyncEnumerable<Post> ReadPostsAsync(
        string arhiveFilePath,
        bool skipContent,
        CancellationToken ct
    );
}
