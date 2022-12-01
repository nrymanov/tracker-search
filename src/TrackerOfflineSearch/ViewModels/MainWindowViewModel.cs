using Microsoft.Extensions.Logging;
using Prism.Events;

namespace TrackerOfflineSearch.ViewModels;

public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
{
    public MainWindowViewModel(IEventAggregator eventAggregator, ILogger<MainWindowViewModel> logger) : base(eventAggregator, logger)
    {
    }
}
