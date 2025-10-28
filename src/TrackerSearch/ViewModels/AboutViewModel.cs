using System.Reactive.Linq;

namespace TrackerSearch.ViewModels;

public class AboutViewModel : ActivatableViewModel, IAboutViewModel
{
    public AboutViewModel()
    {
        // Implementation for SearchViewModel
    }

    public IObservable<bool> IsBusy => Observable.Return(value: false);
}
