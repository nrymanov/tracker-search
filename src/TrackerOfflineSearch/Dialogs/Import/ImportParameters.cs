using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Dialogs.Import;

public record ImportParameters(
    string ArchivePath,
    bool SimpleIndex,
    IndexOptimizationStrategy IndexOptimization
    );

