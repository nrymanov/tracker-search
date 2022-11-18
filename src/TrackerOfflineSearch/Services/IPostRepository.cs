using System;
using System.Collections.Generic;
using DynamicData;
using Lucene.Net.Search;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services;

public interface IPostRepository
{
    int TotalItems { get; }

    void Search(Query query);

    IObservable<IChangeSet<Post>> Connect();

    IReadOnlyList<string> Forums { get; }

    IWriteSession NewWriteSession();
}
