namespace TrackerSearch.ViewModels;

public class LoadingViewModel : ActivatableViewModel, ILoadingViewModel
{
    public LoadingViewModel()
    {
    }

    public IObservable<bool> IsBusy => Observable.Return(value: false);
}
