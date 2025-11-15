using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace TrackerOfflineSearch;

public static class AppConsts
{
    public const LuceneVersion SearchEngineVersion = LuceneVersion.LUCENE_48;

    public const DateResolution DefaultDateResolution = DateResolution.DAY;

    public const string ApplicationName = "TrackerOfflineSearch";

    public const string LogsDir = "Logs";

    public const string IndexDir = "Index";

    public const int RAMBufferSizeMB = 64;
}
