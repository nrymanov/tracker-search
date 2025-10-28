using System.Windows;

namespace TrackerOfflineSearch.Settings;

public class Placement : IPlacement
{
    #region Constructor

    public Placement()
    {
    }

    #endregion

    #region IPlacement implementation

    public Rect Location
    {
        get;
        set;
    } = Rect.Empty;

    public WindowState State
    {
        get;
        set;
    }

    #endregion
}
