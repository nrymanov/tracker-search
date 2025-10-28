namespace TrackerSearch.ViewModels;

public interface IApplicationPage
{
    IObservable<bool> IsBusy { get; }
}
