using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Dialogs.Import;

[ExcludeFromCodeCoverage]
public record ImportParameters(
    string ArchivePath,
    bool SimpleIndex,
    IndexOptimizationStrategy IndexOptimization
    );

