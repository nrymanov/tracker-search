using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TrackerOfflineSearch.Settings;

public interface IAppSettings 
{
    string AppName { get; }

    string AppDataDirectory { get; }

    string MainIndexPath { get; }

    bool Portable { get; }

    int ChunkSize { get; }

    double RAMBufferSizeMB { get; }

    IDictionary<string, IPlacement> Positions { get; }

    void Save();
}

public class AppSettings : IAppSettings
{
    private const string SETTINGS_FILE_NAME = "appsettings.json";

    private class StoredSettings
    {
        public bool Portable { get; set; } = false;

        public int ChunkSize { get; set; } = 1_000;

        public double RAMBufferSizeMB { get; set; } = 1024.0;

        public IDictionary<string, IPlacement> Positions { get; } = new Dictionary<string, IPlacement>();
    }

    public AppSettings()
    {
        this.AppName = this.GetType().Assembly.GetName().Name;

        var appDirectory = Path.TrimEndingDirectorySeparator(AppDomain.CurrentDomain.BaseDirectory);
        var appConfigPath = Path.Combine(appDirectory, SETTINGS_FILE_NAME);
        
        using FileStream createStream = File.OpenRead(appConfigPath);
        var stored = JsonSerializer.Deserialize<StoredSettings>(createStream);

        //this.fs = fs ?? throw new System.ArgumentNullException(nameof(fs));

        this.Portable = false;
        this.ChunkSize = 1_000;
        this.RAMBufferSizeMB = 1024.0;
        this.Positions = new Dictionary<string, IPlacement>();
    }

    public string AppName { get; }

    public string AppDataDirectory { get; }

    public string MainIndexPath { get; }

    public bool Portable { get; }

    public int ChunkSize { get; }

    public double RAMBufferSizeMB { get; }

    public IDictionary<string, IPlacement> Positions { get; }

    public void Save()
    { 
    }

    //private readonly IFileSystem fs;
}

/*
public class AppSettings
{
    //"Application": {
    //    "Portable": true,
    //    "Import": {
    //        "ChunkSize": 5000
    //    },
    //    "Lucene": {
    //      "RAMBufferSizeMB": 1024
    //    }
    //}

    public class ImportSettings
    {
        public int ChunkSize { get; init; } = 1_000;
    }
    
    public class LuceneSettings
    {
        public double RAMBufferSizeMB { get; init; } = 1024.0;
    }

    public bool Portable { get; init; }

    public ImportSettings Import { get; init; }

    public LuceneSettings Lucene { get; init; }
}
*/
