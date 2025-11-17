namespace TrackerOfflineSearch.Services;

public interface IBackgroundRunner
{
    Task RunAsync(Action action, CancellationToken ct = default);
}
