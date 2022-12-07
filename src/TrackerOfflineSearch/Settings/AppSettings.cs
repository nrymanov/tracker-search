namespace TrackerOfflineSearch.Settings;

public class AppSettings
{
    //"Application": {
    //    "Portable": true,
    //    "Import": {
    //        "ChunkSize": 5000
    //    },
    //    "Lucene": {
    //      "RAMBufferSizeMB": 1024
    //    }
    //}

    public class ImportSettings
    {
        public int ChunkSize { get; init; } = 1_000;
    }
    
    public class LuceneSettings
    {
        public double RAMBufferSizeMB { get; init; } = 1024.0;
    }

    public bool Portable { get; init; }

    public ImportSettings Import { get; init; }

    public LuceneSettings Lucene { get; init; }
}
