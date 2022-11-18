using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace TrackerOfflineSearch;

public static class AppConst
{
    public const LuceneVersion SearchEngineVersion = LuceneVersion.LUCENE_48;

    public const DateResolution DefaultDateResolution = DateResolution.DAY;

    public const string IndexName = "mainIndex";
}
