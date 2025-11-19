using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services;

public interface IIndexWriterSession : IDisposable
{
    Task ClearAsync(CancellationToken cancellation);

    void Add(Post post);

    Task OptimizeAsync(IndexOptimizationStrategy strategy, CancellationToken cancellation);

    Task CommitAsync(CancellationToken cancellation);

    bool HasChanges { get; }
}
