using System;
using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace TrackerOfflineSearch.Services.Implementation;

internal static class QueryBuilderHelpers
{
    public static string? DateToString(this DateTime dt) => DateTools.DateToString(dt, AppConst.DefaultDateResolution);

    public static BytesRef? ToBytesRef(this DateTime? dt) => dt.HasValue ? new BytesRef(dt.Value.DateToString()) : null;
}
