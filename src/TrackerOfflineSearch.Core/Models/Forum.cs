namespace TrackerOfflineSearch.Core.Models;

public record Forum
{
    public const string Separator = " - ";

    public static readonly Forum AllForums = new();

    private Forum()
    {
        Id = Separator;
        ParentId = "";
        Name = "Все форумы";
        Order = 0;
    }

    public Forum(string fullPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullPath);

        Id = fullPath;
        var lastSeparatorIndex = fullPath.LastIndexOf(Separator);
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
    }

    public string Id { get; }

    public string ParentId { get; }

    public string Name { get; }

    public int Order { get; }

    public bool IsChildOf(Forum forum) =>
        Id.StartsWith($"{forum.Id}{Separator}", StringComparison.Ordinal);

}
