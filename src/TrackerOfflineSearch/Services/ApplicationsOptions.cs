namespace TrackerOfflineSearch.Services;

public class ApplicationsOptions
{
    public string IndexPath { get; set; } = null!;

    public int RAMBufferSizeMB { get; set; }
}
