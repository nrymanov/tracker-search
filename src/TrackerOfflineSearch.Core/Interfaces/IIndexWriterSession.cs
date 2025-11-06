using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IIndexWriterSession : IDisposable
{
    Task ClearAsync(CancellationToken cancellation);

    void Add(Post post);

    Task OptimizeAsync(IndexOptimizationStrategy strategy, CancellationToken cancellation);

    Task CommitAsync(CancellationToken cancellation);
}
