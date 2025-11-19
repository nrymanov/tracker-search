using System.Reactive.Linq;
using ReactiveUI;
using Splat;

namespace TrackerOfflineSearch.UnitTests.Helpers;

public sealed class DisableReactiveUIActivation : IDisposable
{
    private readonly IActivationForViewFetcher? _original;

    public DisableReactiveUIActivation()
    {
        // Save original
        _original = Locator.Current.GetService<IActivationForViewFetcher>();
        if (_original is not null)
        {
            Locator.CurrentMutable.UnregisterCurrent<IActivationForViewFetcher>();
        }

        // Replace with no-op fetcher
        Locator.CurrentMutable.RegisterLazySingleton<IActivationForViewFetcher>(
            () => new StubActivationForViewFetcher()
        );
    }

    public void Dispose()
    {
        // Restore original
        Locator.CurrentMutable.UnregisterCurrent<IActivationForViewFetcher>();

        if (_original is not null)
        {
            Locator.CurrentMutable.RegisterLazySingleton<IActivationForViewFetcher>(() => _original);
        }
    }

    private sealed class StubActivationForViewFetcher : IActivationForViewFetcher
    {
        public int GetAffinityForView(Type view) => 10;

        public IObservable<bool> GetActivationForView(IActivatableView view) => Observable.Return(true);
    }
}
