using System.Xml.Linq;
using Lucene.Net.Documents;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Interfaces;

public interface IPostMapper
{
    Post Map(XElement el);

    Post Map(Document doc, int index);

    //Document Map(Post doc);
}
