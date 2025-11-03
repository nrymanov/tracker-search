namespace TrackerSearch.Dialogs.Import;

public record ImportResult(
    ImportParameters Parameters,
    int TotalDocuments,
    TimeSpan Elapsed
    );

