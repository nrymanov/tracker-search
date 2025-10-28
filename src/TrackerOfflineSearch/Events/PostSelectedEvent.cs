using Prism.Events;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Events;

public class PostSelectedEvent : PubSubEvent<Post?> { }
