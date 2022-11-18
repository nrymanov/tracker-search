using System.Collections;
using System.Text.RegularExpressions;
using BBParser;

namespace BBParserTests;

[TestFixture]
public class BBCodeParserTests
{
    //[SetUp]
    //public void Setup()
    //{
    //}

    private static string BBText1 =
@"<span style=""font-size: 24px; line-height: normal;"">Гомотрон/Игра престолов, 5 сезон.</span><br><img class=""post-image post-image-aligned post-image-aligned-right"" src=""https://i.ytimg.com/vi/YJgoz239A6s/mqdefault.jpg""><br><span class=""post-bold"">Тип записи</span>: аудиоверсия<br><span class=""post-bold"">Страна (Издатель)</span>: Россия<br><span class=""post-bold"">Автор</span>: Дмитрий Пучков<br><span class=""post-bold"">Исполнитель</span>: Дмитрий Пучков, Клим Жуков<br><span class=""post-bold"">Год</span>: 2022<br><span class=""post-bold"">Формат</span>: мп3<br><span class=""post-bold"">Битрейт</span>: 90 кб/с<br><span class=""post-bold"">Средняя продолжительность выпуска</span>: 1:51:12<br><span class=""post-bold"">Общая продолжительность раздачи</span>: 9:24:36<br><span class=""post-bold"">Описание</span>: комедийный разбор сериала<br><span class=""post-bold"">Дополнительно</span> раздача будет обновляться по мере выхода эпизодов.<br><div class=""sp-wrap clearfix""><div class=""sp-head unfolded""><span>Треклист</span></div><div class=""sp-body clearfix"">[pre]<br>01 с5 э1 «Грядущие войны».mp3 1:48:08<br>02 с5 э2 «Чёрно-Белый Дом».mp3 2:01:46<br>03 с5 э3 «Его Воробейшество» .mp3 1:53:54<br>04 с5 э4 «Сыны Гарпии» .mp3 1:47:49<br>05 с5 э5 «Убей мальчишку» 1:52:58<br>[/pre]</div></div><br><span class=""post-bold"">[color=green]сезоны 1, 2: </span>https://rutracker.org/forum/viewtopic.php?t=6174729<br><span class=""post-bold"">[color=green]сезон 3:</span> https://rutracker.org/forum/viewtopic.php?t=6189512<br><span class=""post-bold"">[color=green]сезон 4:</span> https://rutracker.org/forum/viewtopic.php?t=6214074<br>сезон 5, эта раздача.";

    private class TagReplacer
    {
        private readonly Regex regex;
        private readonly string replacement;

        public TagReplacer(string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase)
        {
            this.regex = new Regex(pattern, options);
            this.replacement = replacement;
        }

        public string Replace(string bbText) => this.regex.Replace(bbText, this.replacement);
        
    }
    private static readonly TagReplacer[] simpleTags = { 
        new TagReplacer(@"\[br\]", "<br>"),
        new TagReplacer(@"\[hr\]", "<hr>"),

        // Images
        new TagReplacer(@"\[img\](?<value>.*?)\[/img\]",                             "<img class=\"post-image\" src=\"${value}\">"),
        new TagReplacer(@"\[img=(?<align>center|left|right)\](?<value>.*?)\[/img\]", "<img class=\"post-image post-image-aligned post-image-aligned-${align}\" src=\"${value}\">"),

        // Urls
        new TagReplacer(@"\[url=(?<path>[^\]]+)\](?<value>.*?)\[/url\]", "<a href=\"${path}\" class=\"post-link\">${value}</a>"),

        // Text styles
        new TagReplacer(@"\[b\](?<value>.*?)\[/b\]", "<span class=\"post-bold\">${value}</span>"),
        new TagReplacer(@"\[i\](?<value>.*?)\[/i\]", "<span class=\"post-italic\">${value}</span>"),
        new TagReplacer(@"\[u\](?<value>.*?)\[/u\]", "<span class=\"post-underline\">${value}</span>"),
        new TagReplacer(@"\[s\](?<value>.*?)\[/s\]", "<span class=\"post-strikethrough\">${value}</span>"),

        new TagReplacer(@"\[color=(?<color>[^\]]+)\](?<value>.*?)\[/color\]", "<span style=\"color: ${color};\">${value}</span>"),
        new TagReplacer(@"\[size=(?<size>\d+)\](?<value>.*?)\[/size\]",       "<span style=\"font-size: ${size}px; line-height: normal;\">${value}</span>"),
        new TagReplacer(@"\[font=(?<font>[^\]]+)\](?<value>.*?)\[/font\]",    "<span class=\"post-font-${font}\">${value}</span>"),

        // Align
        new TagReplacer(@"\[align=(?<align>center|left|right)\](?<value>.*?)\[/align\]", "<span class=\"post-align post-align-${align}\">${value}</span>"),

        // Lists
        new TagReplacer(@"\[\*\](?<value>.*?)(?=\[(\*|/list)\])",         "<li>${value}</li>"),
        new TagReplacer(@"\[list\](?<value>.*?)\[/list\]",                "<ol>${value}</ol>"),
        new TagReplacer(@"\[list=(?<type>1|a|i)\](?<value>.*?)\[/list\]", "<ol type=\"${type}\">${value}</ol>"),
        
        // Spoiler
        new TagReplacer(@"\[spoiler=\""(?<title>.*?)\""\](?<value>.*?)\[/spoiler\]", "<div class=\"sp-wrap clearfix\"><div class=\"sp-head unfolded\"><span>${title}</span></div><div class=\"sp-body clearfix\">${value}</div></div>"),
        
        // Box
        new TagReplacer(@"\[box\](?<value>.*?)\[/box\]", "<div class=\"post-box-default\"><div class=\"post-box\">${value}</div></div>"),
        //new TagReplacer(@"", ""),
    };

    [Test]
    public void NullInput_Throw()
    {
        var converter = new BBTextConverter();

        var outText = converter.Convert(BBText1);


        // \r\n => \n, \n => <br>
        var bbText = BBText1.Replace("\r\n", "\n").Replace("\n", "<br>");

        foreach (var st in simpleTags)
        {
            bbText = st.Replace(bbText);
        }

    }
}


/*
 
 namespace Test
{
  static class Program
  {
    static String s_pattern = "\\[img(?:=\"(?<defattr>[^\"]+)\")?(?:\\s+(?<attr>\\w+)=\"(?<attrvalue>[^\"]+)\")*\\](?<valu" +
        "e>.+?)\\[/img\\]";
    static RegexOptions s_options = RegexOptions.Multiline;
    static String s_input = "[img=\"source\" width=\"100\" height=\"50\" alt=\"Lubeck city gate\" title=\"This is one <" +
        "br>of the medieval city gates of <br>Lubeck\"]https://www.bbcode.org/images/lubec" +
        "k_small.jpg[/img]";
    static void Main(String[] args)
    {
      var regex = new Regex(s_pattern, s_options, TimeSpan.FromMilliseconds(1000));

      var replacement = "<img src=\"${value}\">";
      var result = regex.Replace(s_input, replacement);
      Console.WriteLine(result);
    }
  }
}
 
 
 */