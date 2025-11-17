namespace TrackerOfflineSearch.Services.Implementation;

public class TaskBackgroundRunner : IBackgroundRunner
{
    public Task RunAsync(Action action, CancellationToken ct = default) =>
        Task.Run(action, ct);
}
