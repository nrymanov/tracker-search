using System.IO;

namespace TrackerOfflineSearch.Services.Implementation;

public sealed class XZXmlStreamFactory : IXmlStreamFactory
{
    public Stream GetStream(string path) => new XZStreamWrapper(path);
}
