using System;
using System.IO;
using Microsoft.Extensions.Options;
using TrackerOfflineSearch.Settings;

namespace TrackerOfflineSearch.Services.Implementation;

public class FileSystem : IFileSystem
{
    #region Constructor

    public FileSystem(IOptions<AppSettings> settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        this.AppName = this.GetType().Assembly.GetName().Name;

        if (/*this.settings.Value.Portable*/ false)
        {
            this.AppDataDirectory = Path.TrimEndingDirectorySeparator(AppDomain.CurrentDomain.BaseDirectory);
        }
        else 
        {
            var baseDirPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            this.AppDataDirectory = Path.Combine(baseDirPath, this.AppName);
        }

        this.MainIndexPath = Path.Combine(this.AppDataDirectory, AppConst.IndexName);
    }

    #endregion

    #region IFileSystem implementation

    public string AppName { get; }

    public string AppDataDirectory { get; }

    public string MainIndexPath { get; }

    #endregion

    #region Private fields & methods

    private readonly IOptions<AppSettings> settings;

    #endregion
}
