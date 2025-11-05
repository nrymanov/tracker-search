namespace TrackerSearch.Dialogs.Import;

public abstract record ImportResult(ImportParameters Parameters);

public record ImportCompletedResult(
    ImportParameters Parameters,
    int TotalDocuments,
    TimeSpan Elapsed
    ) : ImportResult(Parameters);

public record ImportFailedResult(
    ImportParameters Parameters,
    Exception Error
    ) : ImportResult(Parameters);
