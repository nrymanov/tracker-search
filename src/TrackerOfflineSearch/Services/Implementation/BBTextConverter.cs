using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TrackerOfflineSearch.Services.Implementation;

public class BBTextConverter : IBBTextConverter
{
    #region Constructor

    public BBTextConverter()
    {
        this.tagReplacers = new SimpleReplacer[] {
            new SimpleReplacer(@"\[br\]", "<br>"),
            new SimpleReplacer(@"\[hr\]", "<hr>"),

            // Images
            new SimpleReplacer(@"\[img\](?<value>.*?)\[/img\]",                             "<img class=\"post-image\" src=\"${value}\">"),
            new SimpleReplacer(@"\[img=(?<align>center|left|right)\](?<value>.*?)\[/img\]", "<img class=\"post-image post-image-aligned post-image-aligned-${align}\" src=\"${value}\">"),

            // Urls
            new SimpleReplacer(@"\[url=(?<path>[^\]]+)\]", "<a href=\"${path}\" class=\"post-link\">"),
            new SimpleReplacer(@"\[/url\]", "</a>"),

            // Text styles
            new SimpleReplacer(@"\[b\]", "<span class=\"post-bold\">"),
            new SimpleReplacer(@"\[/b\]", "</span>"),

            new SimpleReplacer(@"\[i\]", "<span class=\"post-italic\">"),
            new SimpleReplacer(@"\[/i\]", "</span>"),

            new SimpleReplacer(@"\[u\]", "<span class=\"post-underline\">"),
            new SimpleReplacer(@"\[/u\]", "</span>"),

            new SimpleReplacer(@"\[s\]", "<span class=\"post-strikethrough\">"),
            new SimpleReplacer(@"\[/s\]", "</span>"),

            // [color=colorName|colorCode]
            new SimpleReplacer(@"\[color=(?<color>[^\]]+)\]", "<span style=\"color: ${color};\">"),
            new SimpleReplacer(@"\[/color\]", "</span>"),
            
            // [size=fontsize]
            new SimpleReplacer(@"\[size=(?<size>\d+)\]", "<span style=\"font-size: ${size}px; line-height: normal;\">"),
            new SimpleReplacer(@"\[/size\]",             "</span>"),

            // [pre]
            new SimpleReplacer(@"\[pre\]", "<pre class=\"post-pre\">"),
            new SimpleReplacer(@"\[/pre\]", "</pre>"),

            // [font=fontname]
            new SimpleReplacer(@"\[font=(?<font>[^\]]+)\]", "<span class=\"post-font-${font}\">"),
            new SimpleReplacer(@"\[/font\]",                "</span>"),

            // [align=center|left|right]
            new SimpleReplacer(@"\[align=(?<align>center|left|right)\]", "<span class=\"post-align post-align-${align}\">"),
            new SimpleReplacer(@"\[/align\]", "</span>"),

            // Lists
            new SimpleReplacer(@"\[\*\](?<value>.*?)(?=\[(\*|/list)\])",         "<li>${value}</li>"),
            new SimpleReplacer(@"\[list\](?<value>.*?)\[/list\]",                "<ol>${value}</ol>"),
            new SimpleReplacer(@"\[list=(?<type>1|a|i)\](?<value>.*?)\[/list\]", "<ol type=\"${type}\">${value}</ol>"),
        
            // [spoiler(=spoilerTitle)]
            new SimpleReplacer(@"\[spoiler=\""(?<title>[^""]*?)\""\]", "<div class=\"sp-wrap clearfix\"><div class=\"sp-head folded\"><span>${title}</span></div><div class=\"sp-body clearfix\">"),
            new SimpleReplacer(@"\[spoiler\]",                         "<div class=\"sp-wrap clearfix\"><div class=\"sp-head folded\"><span>скрытый текст</span></div><div class=\"sp-body clearfix\">"),
            new SimpleReplacer(@"\[/spoiler\]", "</div></div>"),
        
            // [box]
            new SimpleReplacer(@"\[box\]", "<div class=\"post-box-default\"><div class=\"post-box\">"),
            new SimpleReplacer(@"\[/box\]", "</div></div>"),
        };

        var assembly = Assembly.GetExecutingAssembly();
        using var resource = assembly.GetManifestResourceStream(this.GetType(), "PostTemplate.html");
        using var reader = new StreamReader(resource);

        this.template = reader.ReadToEnd();
    }

    #endregion

    #region IBBTextConverter implementation

    public string Convert(string bbText)
    {
        // \r\n => \n, \n => <br>

        var postBody = this.tagReplacers.Aggregate(
            bbText.Replace("\r\n", "\n").Replace("\n", "<br>"), 
            (text, replacer) => replacer.Replace(text)
        );

        return this.template.Replace(PostBodyPlaceholder, postBody);
    }

    #endregion

    #region Private fields & methods

    private const string PostBodyPlaceholder = "<!-- Post placeholder -->";

    private class SimpleReplacer
    {
        public SimpleReplacer(string pattern, string replacement)
        {
            this.regex = new Regex(pattern, RegexOptions.IgnoreCase);
            this.replacement = replacement;
        }

        public string Replace(string bbText) => this.regex.Replace(bbText, this.replacement);

        private readonly Regex regex;
        private readonly string replacement;
    }

    private readonly SimpleReplacer[] tagReplacers;
    private readonly string template;

    #endregion
}
