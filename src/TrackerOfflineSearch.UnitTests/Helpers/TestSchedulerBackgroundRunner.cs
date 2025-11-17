using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.UnitTests.Helpers;

public class TestSchedulerBackgroundRunner(TestScheduler scheduler) : IBackgroundRunner
{
    public Task RunAsync(Action action, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource();

        scheduler.Schedule(() =>
        {
            if (!ct.IsCancellationRequested)
            {
                action();
            }

            tcs.SetResult();
        });

        return tcs.Task;
    }

    public Task<T> RunAsync<T>(Func<T> action, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<T>();

        scheduler.Schedule(() =>
        {
            if (ct.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return;
            }

            var result = action();
            tcs.SetResult(result);
        });

        return tcs.Task;
    }
}
