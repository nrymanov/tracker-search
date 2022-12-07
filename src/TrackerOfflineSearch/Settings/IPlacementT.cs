using System.Windows;

namespace TrackerOfflineSearch.Settings;

public interface IPlacement<T> where T: Window
{
    void Attach(T window);
}
