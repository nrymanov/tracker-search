using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.ViewModels;

public class ForumViewModel : ReactiveObject
{
    public ForumViewModel(Node<Forum, string> node, IComparer<ForumViewModel> comparer)
    {
        _node = node;

        Item = _node.Item;

        _node.Children.Connect()
            .Transform(y => new ForumViewModel(y, comparer))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _forums, comparer)
            .Subscribe();
    }

    public Forum Item { get; }

    public string Name => Item.Name;

    public string Id => Item.Id;

    public int Order => Item.Order;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public ReadOnlyObservableCollection<ForumViewModel> Forums => _forums;

    private readonly Node<Forum, string> _node;

    private readonly ReadOnlyObservableCollection<ForumViewModel> _forums;

    private bool _isExpanded;
}
