using System.Threading.Tasks;

namespace TrackerOfflineSearch.Services;

public interface IImportManager
{
    int ImportCount { get; }

    Task ImportAsync(string archivePath);

    Task OptimizeAsync();

    void Cancel();
}
