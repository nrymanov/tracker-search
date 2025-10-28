namespace TrackerSearch.ViewModels;

public interface IImportViewModel : IApplicationPage
{
    IObservable<Unit> ImportCompleted { get; }
}
