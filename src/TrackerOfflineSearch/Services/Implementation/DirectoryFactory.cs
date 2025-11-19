using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TrackerOfflineSearch.Services.Implementation;

public static class DirectoryFactory
{
    public static Directory GetDirectory(IServiceProvider serviceProvider)
    {
        var analyzer = serviceProvider.GetRequiredService<Analyzer>();
        var options = serviceProvider.GetRequiredService<IOptions<ApplicationsOptions>>().Value;

        var indexPath = options.IndexPath;

        if (string.IsNullOrWhiteSpace(indexPath))
        {
            throw new InvalidOperationException("Index path is not configured. Please set the index path before performing this operation.");
        }

        if (!System.IO.Directory.Exists(indexPath))
        {
            System.IO.Directory.CreateDirectory(indexPath);
        }

        var directory = FSDirectory.Open(indexPath);

        if (!DirectoryReader.IndexExists(directory))
        {
            var config = new IndexWriterConfig(AppConsts.SearchEngineVersion, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND,
            };
            using var writer = new IndexWriter(directory, config);
            writer.Commit();
        }

        return directory;
    }
}
