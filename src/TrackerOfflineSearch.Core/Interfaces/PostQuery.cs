namespace TrackerOfflineSearch.Core.Interfaces;

public record class PostQuery(
        string? Title,
        string? Content,
        string? Forum,
        long? MinSize = null,
        long? MaxSize = null,
        DateTime? MinDate = null,
        DateTime? MaxDate = null
    )
{
    public bool HasTitleQuery() => !string.IsNullOrEmpty(Title);

    public bool HasContentQuery() => !string.IsNullOrEmpty(Content);

    public bool HasForumFilter() => !string.IsNullOrEmpty(Forum);

    public bool HasSizeFilter() => MinSize.HasValue || MaxSize.HasValue;

    public bool HasDateFilter() => MinDate.HasValue || MaxDate.HasValue;

    public bool IsEmpty => !(HasTitleQuery() || HasContentQuery() || HasForumFilter() || HasSizeFilter() || HasDateFilter());
}
