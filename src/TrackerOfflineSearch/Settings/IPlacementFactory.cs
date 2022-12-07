namespace TrackerOfflineSearch.Settings;

public interface IPlacementFactory
{ 
    IPlacement GetPlacement(string key);
}
