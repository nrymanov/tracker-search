using System.IO;

namespace TrackerOfflineSearch.Services;

public interface IXmlStreamFactory
{
    Stream GetStream(string path);
}

