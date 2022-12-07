using System.Collections.Generic;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services;

public interface IArchiveManager
{
    IEnumerable<Post> GetPosts(string archivePath);
}
