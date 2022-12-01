namespace TrackerOfflineSearch.Services;

public record class PostQuery(
        string? TitleQuery, string? ContentQuery,
        string? ForumFilter,
        long? FromSizeFilter, long? ToSizeFilter,
        IDateInterval Interval
    )
{
    public bool HasTitleQuery => !string.IsNullOrEmpty(this.TitleQuery);

    public bool HasContentQuery => !string.IsNullOrEmpty(this.ContentQuery);

    public bool HasForumFilter => !string.IsNullOrEmpty(this.ForumFilter);

    public bool HasSizeFilter => this.FromSizeFilter.HasValue || this.ToSizeFilter.HasValue;

    public bool HasDateFilter
    {
        get 
        { 
            if (this.Interval is null)
                return false;
            if (this.Interval.Kind == DateIntervalKind.None)
                return false;

            var (d1, d2) = this.Interval.Dates;
            return d1.HasValue || d2.HasValue;
        }
    }

    public bool HasFilter => this.HasForumFilter || this.HasSizeFilter || this.HasDateFilter;

    public bool IsEmpty => !this.HasTitleQuery && !this.HasContentQuery && !this.HasFilter;
}
