namespace TrackerOfflineSearch.Services;

public interface IFileSystem
{
    string AppName { get; }

    string AppDataDirectory { get; }

    string MainIndexPath { get; }
}
