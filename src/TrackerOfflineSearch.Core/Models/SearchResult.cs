namespace TrackerOfflineSearch.Core.Models;

public record SearchResult(IEnumerable<Post> Items, int TotalHits);
