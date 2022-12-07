using System;
using ReactiveUI;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.ViewModels;

public class PostCellViewModel : ReactiveObject
{
    #region Constructor

    public PostCellViewModel(Post post)
    {
        this.Post = post ?? throw new ArgumentNullException(nameof(post));
        this.Title = post.Title;
        this.ForumName = post.ForumName;
        this.Created = post.Created;
        this.Size = post.Size;
    }

    #endregion

    #region Public properties & methods

    public Post Post { get; }

    public string Title { get; }

    public string ForumName { get; }

    public DateTime Created { get; }

    public long Size { get; }

    #endregion
}
