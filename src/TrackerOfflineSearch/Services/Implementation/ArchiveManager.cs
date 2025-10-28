using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using TrackerOfflineSearch.Core.Models;
using XZ.NET;

namespace TrackerOfflineSearch.Services.Implementation;

public class ArchiveManager : IArchiveManager
{
    #region Constructor

    public ArchiveManager(IPostMapper mapper)
    {
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    #endregion

    #region IArchiveManager implementation

    public IEnumerable<Post> GetPosts(string archivePath)
    {
        using var stream = File.OpenRead(archivePath);
        using var xzStream = new XZInputStream(stream);
        using var reader = XmlReader.Create(xzStream, new XmlReaderSettings { Async = true });
        
        reader.MoveToContent();

        while (!reader.EOF)
        {
            var post = this.GetPost(reader);
            if (post is null)
                yield break;
            yield return post;
        }
    }

    #endregion

    #region Private fields & methods

    private Post GetPost(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "torrent" && XNode.ReadFrom(reader) is XElement element)
            {
                return this.mapper.ToDomain(element);
            }
        }

        return null;
    }

    private readonly IPostMapper mapper;

    #endregion
}
