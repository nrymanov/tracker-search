namespace TrackerSearch.ViewModels;

public class ActivatableViewModel : ViewModelBase, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
}
