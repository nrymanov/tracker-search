using System;
using System.Web;

namespace TrackerOfflineSearch.Domain;

public record class Post
{
    public const string IdField = nameof(Id);
    public const string CreatedField = nameof(Created);
    public const string SizeField = nameof(Size);
    public const string TitleField = nameof(Title);
    public const string ContentField = nameof(Content);
    public const string HashField = nameof(Hash);
    public const string TrackerIdField = nameof(TrackerId);
    public const string ForumIdField = nameof(ForumId);
    public const string ForumNameField = nameof(ForumName);

    private const string PostUrlTemplate = "https://rutracker.org/forum/viewtopic.php?t={0}";
    private const string TorrentUrlTemplate = "https://rutracker.org/forum/dl.php?t={0}";
    private const string Tracker1Template = "http://bt.t-ru.org/ann?magnet";
    private const string TrackerNTemplate = "http://bt{0}.t-ru.org/ann?magnet";
    private const string MagnetUrlTemplate = "magnet:?xt=urn:btih:{0}&tr={1}&dn={2}";

    public int Id { get; init; }
    public DateTime Created { get; init; }
    public long Size { get; init; }

    public string Title { get; init; }

    public string Content { get; init; }

    public string Hash { get; init; }
    public int TrackerId { get; init; }

    public int ForumId { get; init; }
    public string ForumName { get; init; }

    public string Url => string.Format(PostUrlTemplate, this.Id);

    public string TorrentUrl => string.Format(TorrentUrlTemplate, this.Id);

    public string MagnetUrl
    {
        get 
        {
            var tracker = HttpUtility.UrlEncode(string.Format((this.TrackerId == 1) ? Tracker1Template : TrackerNTemplate, this.TrackerId));
            var title = HttpUtility.UrlEncode(this.Title);

            return string.Format(MagnetUrlTemplate, this.Hash, tracker, title);
        }
    }
}
