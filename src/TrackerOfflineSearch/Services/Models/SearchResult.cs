namespace TrackerOfflineSearch.Services.Models;

public record SearchResult(IEnumerable<Post> Items, int TotalHits);
