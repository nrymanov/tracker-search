using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackerOfflineSearch.Domain;
using TrackerOfflineSearch.Helpers;
using TrackerOfflineSearch.Settings;

namespace TrackerOfflineSearch.Services.Implementation;

public class PostRepositoryWriter : IPostRepositoryWriter
{
    #region Constructor

    public PostRepositoryWriter(
        IOptions<AppSettings> settings,
        IFileSystem fs, 
        Analyzer analyzer, 
        IPostMapper mapper, 
        ILogger<PostRepository> logger
        )
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.indexPath = fs?.MainIndexPath ?? throw new ArgumentNullException(nameof(fs));
        this.analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var indexConfig = new IndexWriterConfig(AppConst.SearchEngineVersion, this.analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
            RAMBufferSizeMB = this.settings.Value.Lucene.RAMBufferSizeMB
        };

        this.directory = FSDirectory.Open(this.indexPath);
        this.writer = new IndexWriter(this.directory, indexConfig);
    }

    #endregion

    #region IPostRepositoryWriter implementation

    public void DeleteAll()
    {
        using var p = Profiler.Start(this.logger);
        this.writer.DeleteAll();
    }

    //public RAMDirectory CreateChunk(Post[] posts)
    //{
    //    using var p = Profiler.Start(this.logger);
    //    var dir = new RAMDirectory();
    //    var indexConfig = new IndexWriterConfig(AppConst.SearchEngineVersion, this.analyzer)
    //    {
    //        OpenMode = OpenMode.CREATE_OR_APPEND,
    //    };
    //    using var writer = new IndexWriter(dir, indexConfig);
    //    writer.AddDocuments(posts.Select(this.mapper.ToRepository));
    //    return dir;
    //}

    //public int Add(RAMDirectory index)
    //{
    //    using var p = Profiler.Start(this.logger);
    //    if (index is null)
    //        throw new ArgumentNullException(nameof(index));
    //    this.writer.AddIndexes(new[] { index });
    //    return this.writer.MaxDoc;
    //}

    public int Add(IEnumerable<Post> posts)
    {
        using var p = Profiler.Start(this.logger);

        this.writer.AddDocuments(posts.Select(this.mapper.ToRepository));

        return this.writer.MaxDoc;
    }

    public void Optimize()
    {
        using var p = Profiler.Start(this.logger);
        this.writer.ForceMerge(1);
    }

    public void Commit()
    {
        using var p = Profiler.Start(this.logger);
        this.writer.Commit();
    }

    public void Rollback()
    {
        using var p = Profiler.Start(this.logger);
        this.writer.Rollback();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
        this.writer.Dispose();
        this.directory.Dispose();
    }

    #endregion

    #region Private fields & methods

    private readonly string indexPath;
    private readonly IOptions<AppSettings> settings;
    private readonly Analyzer analyzer;
    private readonly IPostMapper mapper;
    private readonly ILogger<PostRepository> logger;
    //private readonly IndexWriterConfig indexConfig;
    private readonly FSDirectory directory;
    private readonly IndexWriter writer;

    #endregion
}
