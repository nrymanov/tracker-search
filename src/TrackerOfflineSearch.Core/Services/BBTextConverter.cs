using System.Text;
using System.Text.RegularExpressions;
using TrackerOfflineSearch.Core.Interfaces;

namespace TrackerOfflineSearch.Core.Services;

public class BBTextConverter : IBBTextConverter
{
    #region Constructor

    public BBTextConverter()
    {
        _tagReplacers = [
            new(@"\[br\]", "<br>"),
            new(@"\[hr\]", "<hr>"),

            // Images
            new(@"\[img\](?<value>.*?)\[/img\]",                             "<img class=\"post-image\" src=\"${value}\">"),
            new(@"\[img=(?<align>center|left|right)\](?<value>.*?)\[/img\]", "<img class=\"post-image post-image-aligned post-image-aligned-${align}\" src=\"${value}\">"),

            // Urls
            new(@"\[url=(?<path>[^\]]+)\]", "<a href=\"${path}\" class=\"post-link\">"),
            new(@"\[/url\]",                "</a>"),

            // Text styles
            new(@"\[b\]",  "<span class=\"post-bold\">"),
            new(@"\[/b\]", "</span>"),

            new(@"\[i\]",  "<span class=\"post-italic\">"),
            new(@"\[/i\]", "</span>"),

            new(@"\[u\]",  "<span class=\"post-underline\">"),
            new(@"\[/u\]", "</span>"),

            new(@"\[s\]",  "<span class=\"post-strikethrough\">"),
            new(@"\[/s\]", "</span>"),

            // [clear]
            new(@"\[clear\]", "<div class=\"clear\">&nbsp;</div>"),

            // [color=colorName|colorCode]
            new(@"\[color=(?<color>[^\]]+)\]", "<span style=\"color: ${color};\">"),
            new(@"\[/color\]",                 "</span>"),
            
            // [size=fontsize]
            new(@"\[size=(?<size>\d+)\]", "<span style=\"font-size: ${size}px; line-height: normal;\">"),
            new(@"\[/size\]",             "</span>"),

            // [pre]
            new(@"\[pre\]",  "<pre class=\"post-pre\">"),
            new(@"\[/pre\]", "</pre>"),

            // [font=fontname]
            new(@"\[font=(?<font>[^\]]+)\]", "<span class=\"post-font-${font}\">"),
            new(@"\[/font\]",                "</span>"),

            // [align=center|left|right|justify]
            new(@"\[align=(?<align>center|left|right|justify)\]", "<span class=\"post-align post-align-${align}\">"),
            new(@"\[/align\]",                                    "</span>"),

            // Lists
            new(@"\[\*\](?<value>.*?)(?=\[(\*|/list)\])",         "<li>${value}</li>"),
            new(@"\[list=(?<type>1|a|i)\]", "<ol type=\"${type}\">"),
            new(@"\[list\]",                "<ol>"),
            new(@"\[/list\]",               "</ol>"),

            // [spoiler(=spoilerTitle)]
            new(@"\[spoiler=\""(?<title>.*?)\""\]", "<div class=\"sp-wrap clearfix\"><div class=\"sp-head folded\"><span>${title}</span></div><div class=\"sp-body clearfix\">"),
            new(@"\[spoiler\]",                     "<div class=\"sp-wrap clearfix\"><div class=\"sp-head folded\"><span>скрытый текст</span></div><div class=\"sp-body clearfix\">"),
            new(@"\[/spoiler\]",                    "</div></div>"),

            // [box=align,bordercolor]
            // [box=align]
            // [box=bordercolor]
            // [box]
            new(@"\[box=(?<align>center|left|right),(?<bordercolor>[^\]]+)\]", "<div class=\"post-box-${align}\"><div class=\"post-box\" style=\"border-color: ${bordercolor};\">"),
            new(@"\[box=(?<align>center|left|right)\]",                        "<div class=\"post-box-${align}\"><div class=\"post-box\">"),
            new(@"\[box=(?<bordercolor>[^\],]+)\]",                            "<div class=\"post-box-default\"><div class=\"post-box\" style=\"border-color: ${bordercolor};\">"),
            new(@"\[box\]",                                                    "<div class=\"post-box-default\"><div class=\"post-box\">"),
            new(@"\[/box\]",                                                   "</div></div>"),
        ];

        var assembly = typeof(BBTextConverter).Assembly;
        using var resource = assembly.GetManifestResourceStream(typeof(BBTextConverter), "PostTemplate.html")
            ?? throw new FileNotFoundException("Post template resource stream not found!", "PostTemplate.html");
        using var reader = new StreamReader(resource, Encoding.UTF8);
        _template = reader.ReadToEnd();
    }

    #endregion

    #region IBBTextConverter implementation

    public string Convert(string bbText)
    {
        // \r\n => \n, \n => <br>

        var postBody = _tagReplacers.Aggregate(
            bbText.Replace("\r\n", "\n").Replace("\n", "<br>"),
            (text, replacer) => replacer.Replace(text)
        );

        return _template.Replace(PostBodyPlaceholder, postBody);
    }

    #endregion

    #region Private fields & methods

    private const string PostBodyPlaceholder = "<!-- Post placeholder -->";

    private class SimpleReplacer
    {
        public SimpleReplacer(string pattern, string replacement)
        {
            _regex = new Regex(pattern, RegexOptions.IgnoreCase);
            _replacement = replacement;
        }

        public string Replace(string bbText) => _regex.Replace(bbText, _replacement);

        private readonly Regex _regex;
        private readonly string _replacement;
    }

    private readonly SimpleReplacer[] _tagReplacers;
    private readonly string _template;

    #endregion
}
