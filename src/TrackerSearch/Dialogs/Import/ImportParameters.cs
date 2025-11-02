using TrackerOfflineSearch.Core.Interfaces;

namespace TrackerSearch.Dialogs.Import;

public record ImportParameters(
    string ArchivePath,
    bool SimpleIndex,
    IndexOptimizationStrategy IndexOptimization
    );

