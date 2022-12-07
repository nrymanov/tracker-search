using System;
using Microsoft.Extensions.Options;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.Settings;

public class PlacementFactory : IPlacementFactory
{
    #region Constructor

    public PlacementFactory(IFileSystem fs, IOptions<AppSettings> settings)
    {
        this.fs = fs ?? throw new ArgumentNullException(nameof(fs));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    #endregion

    #region IPlacementFactory implementation

    public IPlacement GetPlacement(string key) => new Placement(key);

    #endregion

    #region Private fields & methods

    private readonly IFileSystem fs;
    private readonly IOptions<AppSettings> settings;

    #endregion
}
