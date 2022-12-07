using System.Configuration;
using System.Windows;

namespace TrackerOfflineSearch.Settings;

[SettingsGroupName(nameof(Placement))]
public class Placement : ApplicationSettingsBase, IPlacement
{
    #region Constructor

    public Placement(string key)
    {
        this.SettingsKey = key;
    }

    #endregion

    #region IPlacement implementation

    [UserScopedSetting]
    [DefaultSettingValue(null)]
    public Rect Location 
    {
        get => (this[LocationKey] is null) ? Rect.Empty : (Rect)this[LocationKey];
        set => this[LocationKey] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue(null)]
    public WindowState State
    {
        get => (this[StateKey] is null) ? WindowState.Normal : (WindowState)this[StateKey];
        set => this[StateKey] = value;
    }

    #endregion

    #region Private fields & methods

    private const string LocationKey = nameof(Location);
    private const string StateKey = nameof(State);

    #endregion
}
