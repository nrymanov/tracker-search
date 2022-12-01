using System;

namespace TrackerOfflineSearch.Services;

public interface IDateInterval
{ 
    DateIntervalKind Kind { get; }

    (DateTime?, DateTime?) Dates { get; }
}
