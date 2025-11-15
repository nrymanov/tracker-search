using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.ViewModels;

public class PostInfoViewModel : ViewModelBase
{
    public PostInfoViewModel(Post post)
    { 
        _post = post;

        Title = post.Title;
        ForumName = post.ForumName;
        Size = post.Size;
        Created = post.Created;

        if (Uri.TryCreate(post.Url, UriKind.Absolute, out var postUri))
        {
            PostUri = postUri;
        }

        if (Uri.TryCreate(post.TorrentUrl, UriKind.Absolute, out var torrentUri))
        {
            TorrentUri = torrentUri;
        }

        MagnetLink = post.MagnetUrl;
    }

    public string Title { get; }

    public string ForumName { get; init; } = string.Empty;

    public long Size { get; init; }
    
    public DateTime Created { get; init; }

    public Uri? PostUri { get; }

    public Uri? TorrentUri { get; }

    public string MagnetLink { get; }

    private readonly Post _post;
}
