using System.Xml.Linq;
using Lucene.Net.Documents;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Services;

public interface IPostMapper
{
    Post ToDomain(Document doc);

    Post ToDomain(XElement el);

    Document ToRepository(Post torrent);
}
