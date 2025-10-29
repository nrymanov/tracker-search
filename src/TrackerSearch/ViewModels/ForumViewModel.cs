using TrackerOfflineSearch.Core.Models;

namespace TrackerSearch.ViewModels;

public class ForumViewModel : ReactiveObject
{
    public ForumViewModel(Node<Forum, string> node, IComparer<ForumViewModel> comparer)
    {
        _node = node;

        Name = _node.Item.Name;
        Id = _node.Item.Id;
        Order = _node.Item.Order;

        _node.Children.Connect()
            .Transform(y => new ForumViewModel(y, comparer))
            .ObserveOn(RxApp.MainThreadScheduler)
            .SortAndBind(out _forums, comparer)
            .Subscribe();
    }

    public string Name { get; }

    public string Id { get; }

    public int Order { get; }

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
