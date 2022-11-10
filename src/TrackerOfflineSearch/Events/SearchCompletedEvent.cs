using Lucene.Net.Search;
using Prism.Events;

namespace TrackerOfflineSearch.Events;

public class SearchCompletedEvent : PubSubEvent<string> { }
