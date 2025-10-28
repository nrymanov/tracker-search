using System.Collections.Generic;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Services;

public interface IArchiveManager
{
    IEnumerable<Post> GetPosts(string archivePath);
}
