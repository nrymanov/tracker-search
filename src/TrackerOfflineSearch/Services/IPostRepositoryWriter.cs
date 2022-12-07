using System;
using System.Collections.Generic;
using Lucene.Net.Store;
using TrackerOfflineSearch.Domain;

namespace TrackerOfflineSearch.Services;

public interface IPostRepositoryWriter : IDisposable
{
    void DeleteAll();

    int Add(IEnumerable<Post> posts);

    //RAMDirectory CreateChunk(Post[] posts);
    //int Add(RAMDirectory index);

    void Optimize();

    void Commit();

    void Rollback();
}
