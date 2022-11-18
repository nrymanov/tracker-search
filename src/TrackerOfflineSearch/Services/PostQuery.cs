using System;

namespace TrackerOfflineSearch.Services;

public record class PostQuery(
        string? TitleQuery, string? ContentQuery,
        string? ForumFilter,
        long? FromSizeFilter, long? ToSizeFilter,
        DateTime? FromDateFilter, DateTime? ToDateFilter
    )
{
    public bool HasTitleQuery => !string.IsNullOrEmpty(TitleQuery);

    public bool HasContentQuery => !string.IsNullOrEmpty(ContentQuery);

    public bool HasForumFilter => !string.IsNullOrEmpty(ForumFilter);

    public bool HasSizeFilter => FromSizeFilter.HasValue || ToSizeFilter.HasValue;

    public bool HasDateFilter => FromDateFilter.HasValue || ToDateFilter.HasValue;

    public bool HasFilter => HasForumFilter || HasSizeFilter || HasDateFilter;

    public bool IsEmpty => !HasTitleQuery && !HasContentQuery && !HasFilter;
}
