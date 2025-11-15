namespace TrackerOfflineSearch.Dialogs.Import;

public record ImportFailedResult(
    ImportParameters Parameters,
    Exception Error
    ) : ImportResult(Parameters);
