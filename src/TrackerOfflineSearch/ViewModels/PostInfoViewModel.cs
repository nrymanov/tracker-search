using System;
using System.Diagnostics;
using System.Reactive;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Core.Models;
using TrackerOfflineSearch.Events;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class PostInfoViewModel : ViewModelBase<PostInfoViewModel>
{
    #region Constructor

    public PostInfoViewModel(IEventAggregator eventAggregator, IBBTextConverter bbTextConverter, ILogger<PostInfoViewModel> logger) : base(eventAggregator, logger)
    {
        this.bbTextConverter = bbTextConverter ?? throw new ArgumentNullException(nameof(bbTextConverter));

        this.EventAggregator.GetEvent<PostSelectedEvent>().Subscribe(this.OnPostSelected);

        this.LaunchUrlCommand = ReactiveCommand.Create<string>(url =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Error launching {url}", url);
            }
        });
    }

    #endregion

    #region Public proprties & methods

    public string? ForumName
    {
        get => this.forumName;
        private set => this.RaiseAndSetIfChanged(ref this.forumName, value);
    }

    public DateTime Created
    {
        get => this.created;
        private set => this.RaiseAndSetIfChanged(ref this.created, value);
    }

    public long Size
    {
        get => this.size;
        private set => this.RaiseAndSetIfChanged(ref this.size, value);
    }

    public string Content
    {
        get => this.content;
        private set => this.RaiseAndSetIfChanged(ref this.content, value);
    }

    public string? Url
    {
        get => this.url;
        private set => this.RaiseAndSetIfChanged(ref this.url, value);
    }

    public string? TorrentUrl
    {
        get => this.torrentUrl;
        private set => this.RaiseAndSetIfChanged(ref this.torrentUrl, value);
    }

    public string? MagnetUrl
    {
        get => this.magnetUrl;
        private set => this.RaiseAndSetIfChanged(ref this.magnetUrl, value);
    }

    public ReactiveCommand<string, Unit> LaunchUrlCommand { get; }

    #endregion

    #region Private fields & methods

    private void OnPostSelected(Post? post)
    {
        this.Title = post?.Title;
        this.ForumName = post?.ForumName;
        this.Created = post?.Created ?? DateTime.Now;
        this.Size = post?.Size ?? 0;
        this.Url = post?.Url;
        this.TorrentUrl = post?.TorrentUrl;
        this.MagnetUrl = post?.MagnetUrl;

        this.Content = this.bbTextConverter.Convert(post?.Content ?? string.Empty);
    }

    private readonly IBBTextConverter bbTextConverter;
    private string? forumName;
    private DateTime created;
    private long size;
    private string? content;
    private string? url;
    private string? torrentUrl;
    private string? magnetUrl;

    #endregion
}
