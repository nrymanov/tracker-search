using System;
using System.IO;

namespace TrackerOfflineSearch.Services.Implementation;

public class FileSystem : IFileSystem
{
    public FileSystem()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appName = this.GetType().Assembly.GetName().Name;

        this.AppDataDirectory = Path.Combine(localData, appName);
        this.MainIndexPath = Path.Combine(this.AppDataDirectory, AppConst.IndexName);
    }

    public string AppDataDirectory { get; }

    public string MainIndexPath { get; }
}