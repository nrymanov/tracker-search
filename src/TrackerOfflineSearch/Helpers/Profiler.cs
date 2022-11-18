using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace TrackerOfflineSearch.Helpers;

public static class Profiler
{
    public static IDisposable Start<T>(ILogger<T> logger, [CallerMemberName] string memberName = "") where T : class
    {
        return new MethodProfiler<T>(logger, memberName);
    }

    private class MethodProfiler<T> : IDisposable where T : class
    {
        public MethodProfiler(ILogger<T> logger, string memberName)
        {
            if (string.IsNullOrEmpty(memberName))
                throw new ArgumentException($"'{nameof(memberName)}' cannot be null or empty.", nameof(memberName));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.memberName = memberName;

            this.logger.LogDebug("{memberName} started", this.memberName);
            this.sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            this.logger.LogDebug("{memberName} completed in {time}", this.memberName, this.sw.Elapsed);
        }

        private readonly ILogger<T> logger;
        private readonly string memberName;
        private readonly Stopwatch sw;
    }
}
