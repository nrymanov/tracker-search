using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Services;

public class ArchiveReader : IArchiveReader
{
    public ArchiveReader(ILogger<ArchiveReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<Post> ReadPostsAsync(
        string arhiveFilePath,
        bool skipContent,
        [EnumeratorCancellation] CancellationToken ct
        )
    {
        await using var stream = new XZStreamWrapper(arhiveFilePath);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

        await reader.MoveToContentAsync().ConfigureAwait(false);

        //int count = 0;

        while (!reader.EOF)
        {
            ct.ThrowIfCancellationRequested();

            var post = await GetPostAsync(reader, skipContent).ConfigureAwait(false);

            if (post is null)
                yield break;

            yield return post;

            //if (++count > 20_000)
            //{
            //    throw new ArgumentOutOfRangeException(nameof(arhiveFilePath));
            //    //break;
            //}
        }
    }

    #region Private fields & methods

    private static async Task<Post?> GetPostAsync(XmlReader reader, bool skipContent)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "torrent" && XNode.ReadFrom(reader) is XElement element)
            {
                try
                {
                    return MapToPost(element, skipContent);
                }
                catch (Exception)
                {
                    // Log error or handle accordingly
                    return Post.Null;
                }
            }
        }

        return null;
    }

    private static Post MapToPost(XElement el, bool skipContent)
    {
        ArgumentNullException.ThrowIfNull(el);

        var id = (int)GetRequiredAttr(el, "id");
        var created = DateTime.SpecifyKind(DateTime.ParseExact(GetRequiredAttr(el, "registred_at").Value, "yyyy.MM.d H:m:s", null), DateTimeKind.Utc);
        var size = (long)GetRequiredAttr(el, "size");

        var title = GetRequiredElement(el, "title").Value;

        var torrent = GetRequiredElement(el, "torrent");
        var hash = GetRequiredAttr(torrent, "hash").Value;
        var trackerId = (int)GetRequiredAttr(torrent, "tracker_id");

        var forum = GetRequiredElement(el, "forum");
        var forumName = forum.Value;
        var forumId = (int)GetRequiredAttr(forum, "id");

        var content = skipContent ? "" : GetRequiredElement(el, "content").Value;

        //var dir = el.Element("dir");

        return new Post
        {
            Id = id,
            Created = created,
            Size = size,

            Title = title,
            Content = content,

            Hash = hash,
            TrackerId = trackerId,

            ForumId = forumId,
            ForumName = forumName
        };
    }

    private static XElement GetRequiredElement(XElement el, XName name) =>
        el.Element(name) ?? throw new InvalidDataException($"Missing required element '{name}' on element '{el.Name}'. Element XML: {el}");

    private static XAttribute GetRequiredAttr(XElement el, XName name) =>
        el.Attribute(name) ?? throw new InvalidDataException($"Missing required attribute '{name}' on element '{el.Name}'. Element XML: {el}");

    private readonly ILogger<ArchiveReader> _logger;

    #endregion
}
