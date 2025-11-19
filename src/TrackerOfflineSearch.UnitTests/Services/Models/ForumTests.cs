using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.UnitTests.Services.Models;

public class ForumTests
{
    [Fact]
    public void Constructor_Throws_When_FullPath_Is_Null_Or_Empty()
    {
        Assert.ThrowsAny<ArgumentException>(() => new Forum(null!));
        Assert.ThrowsAny<ArgumentException>(() => new Forum(""));
    }

    [Fact]
    public void Constructor_Parses_Forum_Without_Parent()
    {
        var forum = new Forum("Movies");

        Assert.Equal("Movies", forum.Id);
        Assert.Equal("", forum.ParentId);
        Assert.Equal("Movies", forum.Name);
        Assert.Equal(1, forum.Order);
    }

    [Fact]
    public void Constructor_Parses_Forum_With_One_Level_Parent()
    {
        var forum = new Forum("Movies - Action");

        Assert.Equal("Movies - Action", forum.Id);
        Assert.Equal("Movies", forum.ParentId);
        Assert.Equal("Action", forum.Name);
    }

    [Fact]
    public void Constructor_Parses_Forum_With_Two_Level_Parents()
    {
        var forum = new Forum("Movies - Action - 1990");

        Assert.Equal("Movies - Action - 1990", forum.Id);
        Assert.Equal("Movies - Action", forum.ParentId);
        Assert.Equal("1990", forum.Name);
    }

    [Fact]
    public void AllForums_Has_Expected_Default_Values()
    {
        var f = Forum.AllForums;

        Assert.Equal("", f.Id);
        Assert.Equal(" - ", f.ParentId);
        Assert.Equal("Все форумы", f.Name);
        Assert.Equal(0, f.Order);
    }

    // -----------------------------------------------------------
    // IsChildOf tests
    // -----------------------------------------------------------

    [Fact]
    public void IsChildOf_Returns_True_For_Direct_Child()
    {
        var parent = new Forum("Movies");
        var child = new Forum("Movies - Action");

        Assert.True(child.IsChildOf(parent));
    }

    [Fact]
    public void IsChildOf_Returns_True_For_Deep_Child()
    {
        var parent = new Forum("Movies");
        var child = new Forum("Movies - Action - 1990");

        Assert.True(child.IsChildOf(parent));
    }

    [Fact]
    public void IsChildOf_Returns_False_When_Not_A_Child()
    {
        var parent = new Forum("Movies");
        var other = new Forum("Books - Science");

        Assert.False(other.IsChildOf(parent));
    }

    [Fact]
    public void IsChildOf_Returns_False_When_Forum_Is_The_Same()
    {
        var forum = new Forum("Movies");

        Assert.False(forum.IsChildOf(forum));
    }

    [Fact]
    public void IsChildOf_Returns_False_When_Only_Name_Matches()
    {
        var parent = new Forum("News");
        var child = new Forum("Newsline"); // same prefix but not a valid child structure

        Assert.False(child.IsChildOf(parent));
    }

    [Fact]
    public void IsChildOf_Returns_True_For_Root_Forum_With_Parent_AllForums()
    {
        var root = new Forum("Movies");

        Assert.True(root.IsChildOf(Forum.AllForums));
        // because child must be " - Something"
    }

    [Fact]
    public void IsChildOf_Works_With_AllForums()
    {
        var f = new Forum("Anything - Sub");

        Assert.True(f.IsChildOf(Forum.AllForums));
    }
}
