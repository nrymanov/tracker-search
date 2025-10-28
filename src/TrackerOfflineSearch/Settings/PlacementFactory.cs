using System;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.Settings;

public class PlacementFactory : IPlacementFactory
{
    #region Constructor

    public PlacementFactory(IFileSystem fs, IAppSettings settings)
    {
        this.fs = fs ?? throw new ArgumentNullException(nameof(fs));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    #endregion

    #region IPlacementFactory implementation

    public IPlacement GetPlacement(string key)
    {
        if (!this.settings.Positions.TryGetValue(key, out var position))
        {
            position = new Placement();
            this.settings.Positions.Add(key, position);
        }

        return position;
    }

    #endregion

    #region Private fields & methods

    private readonly IFileSystem fs;
    private readonly IAppSettings settings;

    #endregion
}
