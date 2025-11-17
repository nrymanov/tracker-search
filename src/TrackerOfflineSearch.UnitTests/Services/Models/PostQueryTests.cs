using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.UnitTests.Services.Models;

public class PostQueryTests
{
    [Fact]
    public void HasTitleQuery_ReturnsTrue_WhenTitleIsNotEmpty()
    {
        var q = new PostQuery(Title: "abc");
        Assert.True(q.HasTitleQuery());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HasTitleQuery_ReturnsFalse_WhenTitleIsNullOrEmpty(string? value)
    {
        var q = new PostQuery(Title: value);
        Assert.False(q.HasTitleQuery());
    }

    [Fact]
    public void HasContentQuery_ReturnsTrue_WhenContentIsNotEmpty()
    {
        var q = new PostQuery(Content: "content");
        Assert.True(q.HasContentQuery());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HasContentQuery_ReturnsFalse_WhenContentIsNullOrEmpty(string? value)
    {
        var q = new PostQuery(Content: value);
        Assert.False(q.HasContentQuery());
    }

    [Fact]
    public void HasForumFilter_ReturnsTrue_WhenForumIsSpecified()
    {
        var q = new PostQuery(Forum: "123");
        Assert.True(q.HasForumFilter());
    }

    [Fact]
    public void HasForumFilter_ReturnsFalse_WhenForumIsNull()
    {
        var q = new PostQuery(Forum: null);
        Assert.False(q.HasForumFilter());
    }

    [Fact]
    public void HasForumFilter_ReturnsFalse_WhenForumEqualsSeparator()
    {
        var separator = Forum.Separator;
        var q = new PostQuery(Forum: separator);

        Assert.False(q.HasForumFilter());
    }

    [Fact]
    public void HasSizeFilter_ReturnsTrue_WhenMinSizeSpecified()
    {
        var q = new PostQuery(MinSize: 10);
        Assert.True(q.HasSizeFilter());
    }

    [Fact]
    public void HasSizeFilter_ReturnsTrue_WhenMaxSizeSpecified()
    {
        var q = new PostQuery(MaxSize: 100);
        Assert.True(q.HasSizeFilter());
    }

    [Fact]
    public void HasSizeFilter_ReturnsFalse_WhenNoSizeSpecified()
    {
        var q = new PostQuery();
        Assert.False(q.HasSizeFilter());
    }

    [Fact]
    public void HasDateFilter_ReturnsTrue_WhenMinDateSpecified()
    {
        var q = new PostQuery(MinDate: DateTime.UtcNow);
        Assert.True(q.HasDateFilter());
    }

    [Fact]
    public void HasDateFilter_ReturnsTrue_WhenMaxDateSpecified()
    {
        var q = new PostQuery(MaxDate: DateTime.UtcNow);
        Assert.True(q.HasDateFilter());
    }

    [Fact]
    public void HasDateFilter_ReturnsFalse_WhenNoDateSpecified()
    {
        var q = new PostQuery();
        Assert.False(q.HasDateFilter());
    }

    [Fact]
    public void IsEmpty_ReturnsTrue_WhenNoFiltersProvided()
    {
        var q = new PostQuery();
        Assert.True(q.IsEmpty);
    }

    [Theory]
    [InlineData("abc", null, null)]
    [InlineData(null, "content", null)]
    [InlineData(null, null, "forum")]
    public void IsEmpty_ReturnsFalse_WhenAnyFilterProvided(string? title, string? content, string? forum)
    {
        var q = new PostQuery(Title: title, Content: content, Forum: forum);
        Assert.False(q.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenSizeFilterProvided()
    {
        var q = new PostQuery(MinSize: 1);
        Assert.False(q.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenDateFilterProvided()
    {
        var q = new PostQuery(MinDate: DateTime.UtcNow);
        Assert.False(q.IsEmpty);
    }
}
