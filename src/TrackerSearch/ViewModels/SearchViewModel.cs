namespace TrackerSearch.ViewModels;

public class SearchViewModel : ActivatableViewModel, ISearchViewModel
{
    public SearchViewModel()
    {
        TestCommand = ReactiveCommand.CreateFromObservable(() => Observable.Timer(TimeSpan.FromSeconds(5)).Select(_ => Unit.Default));

        IsBusy = TestCommand.IsExecuting;
    }

    public ReactiveCommand<Unit, Unit> TestCommand { get; }

    public IObservable<bool> IsBusy { get; }
}
