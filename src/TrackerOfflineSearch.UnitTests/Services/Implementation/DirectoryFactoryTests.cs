using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Implementation;

namespace TrackerOfflineSearch.UnitTests.Services.Implementation;

public class DirectoryFactoryTests
{
    /// <summary>
    /// Creates a service provider with Analyzer and ApplicationsOptions.
    /// </summary>
    private static IServiceProvider CreateProvider(string? path)
    {
        var services = new ServiceCollection();

        // Analyzer
        services.AddSingleton<Analyzer>(_ => new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48));

        // ApplicationsOptions
        services.AddSingleton<IOptions<ApplicationsOptions>>(
            _ => Options.Create(new ApplicationsOptions { IndexPath = path })
        );

        return services.BuildServiceProvider();
    }

    [Fact]
    public void GetDirectory_Throws_When_IndexPath_Is_Empty()
    {
        // Arrange
        var provider = CreateProvider("");

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            DirectoryFactory.GetDirectory(provider);
        });
    }

    [Fact]
    public void GetDirectory_Creates_Directory_When_Not_Exists()
    {
        // Arrange
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.False(System.IO.Directory.Exists(temp));

        var provider = CreateProvider(temp);

        try
        {
            // Act
            using var dir = DirectoryFactory.GetDirectory(provider);

            // Assert
            Assert.True(System.IO.Directory.Exists(temp));
            Assert.NotNull(dir);
        }
        finally
        {
            System.IO.Directory.Delete(temp, true);
        }
    }

    [Fact]
    public void GetDirectory_Creates_Empty_Index_When_Index_Not_Exists()
    {
        // Arrange
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(temp);
        try
        {
            var provider = CreateProvider(temp);

            // Precondition
            using (var d = FSDirectory.Open(temp))
            {
                Assert.False(DirectoryReader.IndexExists(d));
            }

            // Act
            using var created = DirectoryFactory.GetDirectory(provider);

            // Assert
            using (var d = FSDirectory.Open(temp))
            {
                Assert.True(DirectoryReader.IndexExists(d));
            }
        }
        finally
        {
            System.IO.Directory.Delete(temp, true);
        }
    }

    [Fact]
    public void GetDirectory_Does_Not_Recreate_Index_When_Exists()
    {
        // Arrange
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(temp);
        try
        {
            var provider = CreateProvider(temp);

            // Create initial index manually
            using (var d = FSDirectory.Open(temp))
            {
                using var w = new IndexWriter(d, new IndexWriterConfig(AppConsts.SearchEngineVersion, new StandardAnalyzer(AppConsts.SearchEngineVersion)));
                w.Commit();
            }

            var beforeTimestamp = File.GetLastWriteTimeUtc(Path.Combine(temp, "segments.gen")).Ticks;

            // Act
            using var result = DirectoryFactory.GetDirectory(provider);
            result.Dispose();

            var afterTimestamp = File.GetLastWriteTimeUtc(Path.Combine(temp, "segments.gen")).Ticks;

            // Assert — index must NOT be recreated, so timestamp must remain unchanged
            Assert.Equal(beforeTimestamp, afterTimestamp);
        }
        finally
        {
            System.IO.Directory.Delete(temp, true);
        }
    }
}
