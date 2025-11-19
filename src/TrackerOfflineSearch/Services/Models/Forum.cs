namespace TrackerOfflineSearch.Services.Models;

public record Forum
{
    public const string Separator = " - ";

    public static readonly Forum AllForums = new();

    private Forum()
    {
        Id = "";
        ParentId = Separator;
        Order = 0;
        Name = "Все форумы";
        Path = "";
        SubForumPath = "";
        IsRoot = true;
    }

    public Forum(string fullPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullPath);

        Id = fullPath;
        var lastSeparatorIndex = fullPath.LastIndexOf(Separator, StringComparison.Ordinal);
        if (lastSeparatorIndex >= 0)
        {
            ParentId = fullPath[..lastSeparatorIndex];
            Name = fullPath[(lastSeparatorIndex + Separator.Length)..];
        }
        else
        {
            ParentId = string.Empty;
            Name = fullPath;
        }
        Order = 1;
        Path = $"{Id}";
        SubForumPath = $"{Id}{Separator}";
    }

    public string Id { get; }

    public string ParentId { get; }

    public int Order { get; }

    public string Name { get; }

    public string Path { get; }

    public string SubForumPath { get; }

    public bool IsRoot { get; }

    public bool IsChildOf(Forum forum) =>
        Id.StartsWith(forum.SubForumPath, StringComparison.Ordinal);

}
