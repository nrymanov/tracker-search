using System;
using Lucene.Net.Store;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services;

public interface IWriteSession : IDisposable
{
    void DeleteAll();

    RAMDirectory CreateChunk(Post[] posts);

    int Add(RAMDirectory index);

    void Commit();

    void Rollback();
}
