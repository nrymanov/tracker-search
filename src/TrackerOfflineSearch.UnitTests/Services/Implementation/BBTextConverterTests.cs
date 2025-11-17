using System.Reflection;
using TrackerOfflineSearch.Services.Implementation;

namespace TrackerOfflineSearch.UnitTests.Services.Implementation;

public class BBTextConverterTests
{
    private static BBTextConverter CreateConverterWithTemplate(string template)
    {
        BBTextConverter converter = new();

        // Inject test template (the real constructor loads embedded HTML)
        typeof(BBTextConverter)
            .GetField("_template", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(converter, template);

        return converter;
    }

    [Fact]
    public void Convert_ReplacesPlaceholder()
    {
        var template = "<html><!-- Post placeholder --></html>";
        var converter = CreateConverterWithTemplate(template);

        var result = converter.Convert("text");

        Assert.Equal("<html>text</html>", result);
    }

    [Fact]
    public void Convert_ReplacesNewLines()
    {
        var converter = CreateConverterWithTemplate("X<!-- Post placeholder -->X");

        var result = converter.Convert("a\r\nb\nc");

        Assert.Equal("Xa<br>b<br>cX", result);
    }

    [Fact]
    public void Convert_ReplacesBoldTag()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[b]text[/b]");

        Assert.Equal("<span class=\"post-bold\">text</span>", result);
    }

    [Fact]
    public void Convert_ReplacesItalicTag()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[i]text[/i]");

        Assert.Equal("<span class=\"post-italic\">text</span>", result);
    }

    [Fact]
    public void Convert_ReplacesImage()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[img]path.jpg[/img]");

        Assert.Equal("<img class=\"post-image\" src=\"path.jpg\">", result);
    }

    [Fact]
    public void Convert_ReplacesAlignedImage()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[img=center]a.jpg[/img]");

        Assert.Equal("<img class=\"post-image post-image-aligned post-image-aligned-center\" src=\"a.jpg\">", result);
    }

    [Fact]
    public void Convert_ReplacesUrl()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[url=https://x]text[/url]");

        Assert.Equal("<a href=\"https://x\" class=\"post-link\">text</a>", result);
    }

    [Fact]
    public void Convert_ReplacesColor()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[color=red]text[/color]");

        Assert.Equal("<span style=\"color: red;\">text</span>", result);
    }

    [Fact]
    public void Convert_ReplacesListItems()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[list][*]one[*]two[/list]");

        Assert.Equal("<ol><li>one</li><li>two</li></ol>", result);
    }

    [Fact]
    public void Convert_ReplacesSpoilerWithTitle()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[spoiler=\"abc\"]hello[/spoiler]");

        Assert.Equal(
            "<div class=\"sp-wrap clearfix\"><div class=\"sp-head folded\"><span>abc</span></div><div class=\"sp-body clearfix\">hello</div></div>",
            result);
    }

    [Fact]
    public void Convert_ReplacesBoxWithAlignAndBorder()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[box=center,red]Hi[/box]");

        Assert.Equal(
            "<div class=\"post-box-center\"><div class=\"post-box\" style=\"border-color: red;\">Hi</div></div>",
            result);
    }

    [Fact]
    public void Convert_HandlesMultipleTags()
    {
        var converter = CreateConverterWithTemplate("<!-- Post placeholder -->");

        var result = converter.Convert("[b]Bold[/b] and [i]Italic[/i]");

        Assert.Equal(
            "<span class=\"post-bold\">Bold</span> and <span class=\"post-italic\">Italic</span>",
            result);
    }
}
