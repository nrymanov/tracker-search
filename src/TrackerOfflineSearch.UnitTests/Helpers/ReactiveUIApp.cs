using System.Reactive.Linq;
using ReactiveUI;
using Splat;

namespace TrackerOfflineSearch.UnitTests.Helpers;

public static class ReactiveUIApp
{
    private static readonly AutoResetEvent _rxGate = new(true);

    private sealed class StubActivationForViewFetcher : IActivationForViewFetcher
    {
        public int GetAffinityForView(Type view) => 10;

        public IObservable<bool> GetActivationForView(IActivatableView view) => Observable.Return(true);
    }

    public static void WithFakeActivation(Action block)
    {
        _rxGate.WaitOne();

        try
        {
            var original = Locator.Current.GetService<IActivationForViewFetcher>();
            try
            {
                if (original is not null)
                {
                    Locator.CurrentMutable.UnregisterCurrent<IActivationForViewFetcher>();
                }

                // Replace with no-op fetcher
                Locator.CurrentMutable.RegisterLazySingleton<IActivationForViewFetcher>(
                    () => new StubActivationForViewFetcher()
                );

                block();
            }
            finally
            {
                // Restore original
                Locator.CurrentMutable.UnregisterCurrent<IActivationForViewFetcher>();

                if (original is not null)
                {
                    Locator.CurrentMutable.RegisterLazySingleton<IActivationForViewFetcher>(() => original);
                }
            }
        }
        finally
        {
            _rxGate.Set();
        }
    }
}
