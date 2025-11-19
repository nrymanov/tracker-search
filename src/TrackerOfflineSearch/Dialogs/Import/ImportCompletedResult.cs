namespace TrackerOfflineSearch.Dialogs.Import;

[ExcludeFromCodeCoverage]
public record ImportCompletedResult(
    ImportParameters Parameters,
    int TotalDocuments,
    TimeSpan Elapsed
    ) : ImportResult(Parameters);
