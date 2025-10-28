using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IIndexImportService
{
    void Add(Post post);

    void Clear();

    void Optimize();

    void Commit();

    void Rollback();
}
