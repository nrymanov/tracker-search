using Prism.Events;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Events;

public class ReporitoryChangedEvent : PubSubEvent { }

public class PostSelectedEvent : PubSubEvent<Post> { }