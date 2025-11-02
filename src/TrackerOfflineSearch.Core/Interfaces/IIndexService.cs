using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IIndexService
{
    int TotalCount { get; }

    IEnumerable<Forum> GetForums();

    SearchResult Search(PostQuery postQuery, int limit = 100);

    void Add(Post post);

    void Clear();

    void Optimize(IndexOptimizationStrategy strategy);

    void Commit();

    void Rollback();

    void Refresh();

    Task ClearAsync(CancellationToken cancellation);

    Task OptimizeAsync(IndexOptimizationStrategy strategy, CancellationToken cancellation);

    Task CommitAsync(CancellationToken cancellation);

    Task RollbackAsync(CancellationToken cancellation);
}
