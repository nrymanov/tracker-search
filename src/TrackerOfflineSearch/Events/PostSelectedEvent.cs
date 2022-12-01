using Prism.Events;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Events;

public class PostSelectedEvent : PubSubEvent<Post?> { }
