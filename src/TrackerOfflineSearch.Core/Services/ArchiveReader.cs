using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Services;

public class ArchiveReader : IArchiveReader
{
    public ArchiveReader(ILogger<ArchiveReader> logger, IPostMapper mapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async IAsyncEnumerable<Post> ReadPostsAsync(
        string arhiveFilePath,
        [EnumeratorCancellation] CancellationToken ct
        )
    {
        await using var stream = new XZStreamWrapper(arhiveFilePath);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

        await reader.MoveToContentAsync().ConfigureAwait(false);

        while (!reader.EOF)
        {
            ct.ThrowIfCancellationRequested();

            var post = await GetPostAsync(reader).ConfigureAwait(false);

            if (post is null)
                yield break;

            yield return post;
        }
    }

    #region Private fields & methods

    private async Task<Post?> GetPostAsync(XmlReader reader)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "torrent" && XNode.ReadFrom(reader) is XElement element)
            {
                try
                {
                    return _mapper.Map(element);
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

    private readonly ILogger<ArchiveReader> _logger;
    private readonly IPostMapper _mapper;

    #endregion
}
