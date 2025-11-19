using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services.Implementation;

public class ArchiveReader(ILogger<ArchiveReader> logger, IXmlStreamFactory streamFactory) : IArchiveReader
{
    #region IArchiveReader

    public async IAsyncEnumerable<Post> ReadPostsAsync(
        string arhiveFilePath,
        bool skipContent,
        [EnumeratorCancellation] CancellationToken ct
        )
    {
        var stream = streamFactory.GetStream(arhiveFilePath);
        await using (stream.ConfigureAwait(false))
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

            await reader.MoveToContentAsync().ConfigureAwait(false);

            //int count = 0;

            while (!reader.EOF)
            {
                ct.ThrowIfCancellationRequested();

                var post = await GetPostAsync(reader, skipContent, ct).ConfigureAwait(false);

                if (post is null)
                {
                    yield break;
                }

                if (post.IsNull)
                {
                    continue;
                }

                yield return post;

                //if (++count > 20_000)
                //{
                //    throw new ArgumentOutOfRangeException(nameof(arhiveFilePath));
                //    //break;
                //}
            }
        }
    }

    #endregion

    #region Private fields & methods

    private async Task<Post?> GetPostAsync(XmlReader reader, bool skipContent, CancellationToken token)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (
                reader.NodeType == XmlNodeType.Element &&
                string.Equals(reader.Name, "torrent", StringComparison.Ordinal) &&
                await XNode.ReadFromAsync(reader, token).ConfigureAwait(false) is XElement element
            )
            {
                try
                {
                    return MapToPost(element, skipContent);
                }
                catch (Exception err)
                {
                    _logger.LogError(err, "Failed to map XML element to Post object. XML content: {XmlContent}", element?.ToString() ?? "null");

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
        var created = DateTime.SpecifyKind(DateTime.ParseExact(GetRequiredAttr(el, "registred_at").Value, "yyyy.MM.d H:m:s", provider: null), DateTimeKind.Utc);
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
            ForumName = forumName,
        };
    }

    private static XElement GetRequiredElement(XElement el, XName name) =>
        el.Element(name) ?? throw new InvalidDataException($"Missing required element '{name}' on element '{el.Name}'. Element XML: {el}");

    private static XAttribute GetRequiredAttr(XElement el, XName name) =>
        el.Attribute(name) ?? throw new InvalidDataException($"Missing required attribute '{name}' on element '{el.Name}'. Element XML: {el}");

    private readonly ILogger<ArchiveReader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #endregion
}
