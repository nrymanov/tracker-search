using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Core.Models;
using TrackerOfflineSearch.Core.Services;

namespace XZLibsTest;

internal class Program
{
    private const string ArchivePath = @"E:\Temp\rutracker-20250927.xml.xz";
    //private const string TargetPath = @"E:\Temp\rutracker-20250927.xml";

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

        var mapper = new PostMapper();

        using var indexServer = new LuceneImportService(factory.CreateLogger<LuceneImportService>());
        var ar = new ArchiveReader(factory.CreateLogger<ArchiveReader>(), mapper);

        var channel = Channel.CreateBounded<Post>(new BoundedChannelOptions(100) { SingleReader = false, SingleWriter = true, });
        var writer = channel.Writer;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        indexServer.Clear();

        var consumers = Enumerable.Range(0, 4) // Environment.ProcessorCount * 2
            .Select((_, idx) => WritePostsToIndex(idx, channel.Reader, indexServer))
            .ToArray();

        await foreach (var item in ar.ReadPostsAsync(ArchivePath, CancellationToken.None))
        {
            if (item.IsNull)
            {
                continue;
            }

            await writer.WriteAsync(item);

        }
        writer.Complete();

        await Task.WhenAll(consumers);

        Console.WriteLine($"Index optimization has been started");
        indexServer.Optimize();
        indexServer.Commit();
        Console.WriteLine($"Index optimization has been completed");

        Console.WriteLine($"Elapsed: {sw.Elapsed}");

        Console.ReadLine();
    }

    static async Task WritePostsToIndex(int index, ChannelReader<Post> reader, LuceneImportService indexServer)
    {
        int itemsCount = 0;
        await foreach (var post in reader.ReadAllAsync().ConfigureAwait(false))
        {
            indexServer.Add(post);
            ++itemsCount;
            if (itemsCount % 1000 == 0)
            {
                Console.WriteLine($"{index, 3} Posts added: {itemsCount}");
            }
        }

        Console.WriteLine($"{index,3} Total Posts added: {itemsCount}");
    }

}
