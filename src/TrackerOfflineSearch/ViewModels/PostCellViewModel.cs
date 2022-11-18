using System;
using ReactiveUI;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.ViewModels;

public class PostCellViewModel : ReactiveObject
{
    public PostCellViewModel(Post post)
    {
        this.Post = post ?? throw new ArgumentNullException(nameof(post));
        this.Title = post.Title;
        this.ForumName = post.ForumName;
        this.Created = post.Created;
        this.Size = post.Size;
        //this.Content = post.Content;
        //this.Url = post.Url;
        //this.TorrentUrl = post.TorrentUrl;
        //this.MagnetUrl = post.MagnetUrl;
    }

    public Post Post { get; }

    public string Title { get; }

    public string ForumName { get; }

    public DateTime Created { get; }

    public long Size { get; }

    //public string Content { get;}
 
    //public string Url { get; }

    //public string TorrentUrl { get; }

    //public string MagnetUrl { get; }
}
