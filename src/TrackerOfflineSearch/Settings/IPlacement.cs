using System.Windows;

namespace TrackerOfflineSearch.Settings;

public interface IPlacement
{
    public Rect Location { get; set; }

    public WindowState State { get; set; }

    void Save();
}
