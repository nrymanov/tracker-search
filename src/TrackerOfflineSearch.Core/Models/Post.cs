using System.Web;

namespace TrackerOfflineSearch.Core.Models;

public record Post
{
    public const string IdField = nameof(Id);
    public const string CreatedField = nameof(Created);
    public const string CreatedSortField = nameof(Created) + "_sort";
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

    public static Post Null { get; } = new()
    {
        Id = 0,
        Created = DateTime.MinValue,
        Size = 0,
        Title = string.Empty,
        Content = string.Empty,
        Hash = string.Empty,
        TrackerId = 0,
        ForumId = 0,
        ForumName = string.Empty
    };

    public int Id { get; init; }
    
    public DateTime Created { get; init; }
    
    public long Size { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string Hash { get; init; } = string.Empty;

    public int TrackerId { get; init; }

    public int ForumId { get; init; }
    
    public string ForumName { get; init; } = string.Empty;

    public int Index { get; init; }

    public string Url => string.Format(PostUrlTemplate, Id);

    public string TorrentUrl => string.Format(TorrentUrlTemplate, Id);

    public string MagnetUrl
    {
        get
        {
            var tracker = HttpUtility.UrlEncode(TrackerId == 1 ? Tracker1Template : string.Format(TrackerNTemplate, TrackerId));
            var title = HttpUtility.UrlEncode(Title);

            return string.Format(MagnetUrlTemplate, Hash, tracker, title);
        }
    }

    public bool IsNull => Id == 0;
}
