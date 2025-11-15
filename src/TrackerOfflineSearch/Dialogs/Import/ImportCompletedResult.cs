namespace TrackerOfflineSearch.Dialogs.Import;

public record ImportCompletedResult(
    ImportParameters Parameters,
    int TotalDocuments,
    TimeSpan Elapsed
    ) : ImportResult(Parameters);
