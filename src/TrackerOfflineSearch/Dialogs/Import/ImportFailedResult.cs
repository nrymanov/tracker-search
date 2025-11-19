namespace TrackerOfflineSearch.Dialogs.Import;

[ExcludeFromCodeCoverage]
public record ImportFailedResult(
    ImportParameters Parameters,
    Exception Error
    ) : ImportResult(Parameters);
