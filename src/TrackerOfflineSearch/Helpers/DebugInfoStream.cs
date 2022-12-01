using System;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;

namespace TrackerOfflineSearch.Helpers;

public class DebugInfoStream : InfoStream
{
    public DebugInfoStream(ILogger<DebugInfoStream> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool IsEnabled(string component)
    {
        return this.logger.IsEnabled(LogLevel.Trace);
    }

    public override void Message(string component, string message)
    {
        this.logger.LogTrace("{component}: {message}", component, message);
    }

    private readonly ILogger<DebugInfoStream> logger;

}
