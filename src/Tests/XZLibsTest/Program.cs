using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TrackerOfflineSearch.Core.Models;
using TrackerOfflineSearch.Core.Services;

namespace XZLibsTest;

internal class Program
{
    private const string ArchivePath = @"D:\Projects\rutracker-20230128.xml.xz";

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

        var mapper = new PostMapper();

        using var indexServer = new LuceneIndexService(factory.CreateLogger<LuceneIndexService>(), mapper);
        var ar = new ArchiveReader(factory.CreateLogger<ArchiveReader>(), mapper);

        var channel = Channel.CreateBounded<Post>(new BoundedChannelOptions(100) { SingleReader = false, SingleWriter = true, });
        var writer = channel.Writer;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        indexServer.Clear();

        //var addPostsToIndexTask = WritePostsToIndex(channel.Reader, indexServer);
        var consumers = Enumerable.Range(0, 4) // Environment.ProcessorCount * 2
            .Select(_ => WritePostsToIndex(channel.Reader, indexServer))
            .ToArray();

        await foreach (var item in ar.ReadPostsAsync(args[0], CancellationToken.None))
        {
            if (item.IsNull)
            {
                continue;
            }

            await writer.WriteAsync(item);

        }
        writer.Complete();

        //await addPostsToIndexTask;
        await Task.WhenAll(consumers);

        Console.WriteLine($"Index optimization has been started");
        indexServer.Optimize();
        indexServer.Commit();
        Console.WriteLine($"Index optimization has been completed");

        Console.WriteLine($"Elapsed: {sw.Elapsed}");

        Console.ReadLine();
    }

    static async Task WritePostsToIndex(ChannelReader<Post> reader, LuceneIndexService indexServer)
    {
        int itemsCount = 0;
        await foreach (var post in reader.ReadAllAsync().ConfigureAwait(false))
        {
            indexServer.Add(post);
            ++itemsCount;
            //if (itemsCount % 5000 == 0)
            //{
            //    indexServer.Commit();
            //}
            if (itemsCount % 1000 == 0)
            {
                Console.WriteLine($"Posts added: {itemsCount}");
            }
        }
        //indexServer.Commit();
        Console.WriteLine($"Posts added: {itemsCount}");
    }

}
