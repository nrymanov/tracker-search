using TrackerOfflineSearch.Core.Models;

namespace TrackerSearch.ViewModels;

public class PostInfoViewModel : ViewModelBase
{
    public PostInfoViewModel(Post post)
    { 
        _post = post;

        Title = post.Title;
        ForumName = post.ForumName;
        Size = post.Size;
        Created = post.Created;
    }

    public string Title { get; }

    public string ForumName { get; init; } = string.Empty;

    public long Size { get; init; }
    
    public DateTime Created { get; init; }

    private readonly Post _post;
}
