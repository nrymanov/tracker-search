using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IIndexImportService
{
    void Add(Post post);

    void Clear();

    void Optimize(IndexOptimizationStrategy strategy);

    void Commit();

    void Rollback();

    Task ClearAsync(CancellationToken cancellation);

    Task OptimizeAsync(IndexOptimizationStrategy strategy, CancellationToken cancellation);

    Task CommitAsync(CancellationToken cancellation);

    Task RollbackAsync(CancellationToken cancellation);
}
