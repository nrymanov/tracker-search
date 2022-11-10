using System.Xml.Linq;
using Lucene.Net.Documents;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services;

public interface IPostMapper
{
    Post ToDomain(Document doc);

    Post ToDomain(XElement el);

    Document ToRepository(Post torrent);
}
