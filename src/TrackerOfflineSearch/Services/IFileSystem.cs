namespace TrackerOfflineSearch.Services;

public interface IFileSystem
{ 
    string AppDataDirectory { get; }

    string MainIndexPath { get; }
}