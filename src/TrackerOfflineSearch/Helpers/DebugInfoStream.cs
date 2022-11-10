using System;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;

namespace TrackerOfflineSearch.Helpers;

public class DebugInfoStream : InfoStream
{
    private readonly ILogger<DebugInfoStream> _logger;

    public DebugInfoStream(ILogger<DebugInfoStream> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool IsEnabled(string component)
    {
        return this._logger.IsEnabled(LogLevel.Trace);
    }

    public override void Message(string component, string message)
    {
        this._logger.LogTrace("{component}: {message}", component, message);
    }
}
