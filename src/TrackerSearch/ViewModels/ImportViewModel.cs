using System.Reactive.Linq;

namespace TrackerSearch.ViewModels;

public class ImportViewModel : ActivatableViewModel, IImportViewModel
{
    public ImportViewModel()
    {
        TestCommand = ReactiveCommand.CreateFromObservable(() => Observable.Timer(TimeSpan.FromSeconds(5)).Select(_ => Unit.Default));

        IsBusy = TestCommand.IsExecuting;

        ImportCompleted = TestCommand.AsObservable();
    }

    public ReactiveCommand<Unit, Unit> TestCommand { get; }

    public IObservable<bool> IsBusy { get; }

    public IObservable<Unit> ImportCompleted { get; }
}
