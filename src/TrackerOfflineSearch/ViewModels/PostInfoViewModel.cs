using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using BBParser;
using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;
using TrackerOfflineSearch.Domain;
using TrackerOfflineSearch.Events;

namespace TrackerOfflineSearch.ViewModels;

public class PostInfoViewModel : ViewModelBase<PostInfoViewModel>
{
    private readonly IBBTextConverter _bbTextConverter;
    private string? title;
    private string? forumName;
    private DateTime created;
    private long size;
    private string? content;
    private string? url;
    private string? torrentUrl;
    private string? magnetUrl;

    public PostInfoViewModel(IEventAggregator eventAggregator, IBBTextConverter bbTextConverter, ILogger<PostInfoViewModel> logger) : base(eventAggregator, logger)
    {
        this._bbTextConverter = bbTextConverter ?? throw new ArgumentNullException(nameof(bbTextConverter));

        this.EventAggregator.GetEvent<PostSelectedEvent>().Subscribe(this.OnPostSelected);

        this.LaunchUrlCommand = ReactiveCommand.Create<string>(url =>
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
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

    private void OnPostSelected(Post? post)
    {
        this.Title = post?.Title;
        this.ForumName = post?.ForumName;
        this.Created = post?.Created ?? DateTime.Now;
        this.Size = post?.Size ?? 0;
        this.Url = post?.Url;
        this.TorrentUrl = post?.TorrentUrl;
        this.MagnetUrl = post?.MagnetUrl;

		this.Content = this._bbTextConverter.Convert(post?.Content ?? string.Empty);
    }

    public string? ForumName
    {
        get => forumName;
        private set => this.RaiseAndSetIfChanged(ref this.forumName, value);
    }

    public DateTime Created
    {
        get => created;
        private set => this.RaiseAndSetIfChanged(ref this.created, value);
    }

    public long Size
    {
        get => size;
        private set => this.RaiseAndSetIfChanged(ref this.size, value);
    }

    public string Content
    {
        get => content;
        private set => this.RaiseAndSetIfChanged(ref this.content, value);
    }

    public string? Url
    {
        get => url;
        private set => this.RaiseAndSetIfChanged(ref this.url, value);
    }

    public string? TorrentUrl
    {
        get => torrentUrl;
        private set => this.RaiseAndSetIfChanged(ref this.torrentUrl, value);
    }

    public string? MagnetUrl
    {
        get => magnetUrl;
        private set => this.RaiseAndSetIfChanged(ref this.magnetUrl, value);
    }

    public ReactiveCommand<string, Unit> LaunchUrlCommand { get; }
}
