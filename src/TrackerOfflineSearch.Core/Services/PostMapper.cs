using System.Xml.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Core.Services;

public class PostMapper : IPostMapper
{
    public Post Map(XElement el)
    {
        static XElement GetRequiredElement(XElement el, XName name) =>
            el.Element(name) ?? throw new InvalidDataException($"Missing required element '{name}' on element '{el.Name}'. Element XML: {el}");

        static XAttribute GetRequiredAttr(XElement el, XName name) =>
            el.Attribute(name) ?? throw new InvalidDataException($"Missing required attribute '{name}' on element '{el.Name}'. Element XML: {el}");

        ArgumentNullException.ThrowIfNull(el);

        var id = (int)GetRequiredAttr(el, "id");
        var created = DateTime.SpecifyKind(DateTime.ParseExact(GetRequiredAttr(el, "registred_at").Value, "yyyy.MM.d H:m:s", null), DateTimeKind.Utc);
        var size = (long)GetRequiredAttr(el, "size");

        var title = GetRequiredElement(el, "title").Value;

        var torrent = GetRequiredElement(el, "torrent");
        var hash = GetRequiredAttr(torrent, "hash").Value;
        var trackerId = (int)GetRequiredAttr(torrent, "tracker_id");

        var forum = GetRequiredElement(el, "forum");
        var forumName = forum.Value;
        var forumId = (int)GetRequiredAttr(forum, "id");

        var content = GetRequiredElement(el, "content").Value;

        //var dir = el.Element("dir");

        return new Post
        {
            Id = id,
            Created = created,
            Size = size,

            Title = title,
            Content = content,

            Hash = hash,
            TrackerId = trackerId,

            ForumId = forumId,
            ForumName = forumName
        };
    }

    public Post Map(Document doc, int index)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var created = DateTime.SpecifyKind(DateTools.StringToDate(doc.Get(Post.CreatedField)), DateTimeKind.Utc);

        return new Post
        {
            Id = doc.GetField(Post.IdField).GetInt32ValueOrDefault(),

            Created = created,
            Size = doc.GetField(Post.SizeField).GetInt64ValueOrDefault(),

            Title = doc.Get(Post.TitleField),
            Content = doc.Get(Post.ContentField),

            Hash = doc.Get(Post.HashField),

            TrackerId = doc.GetField(Post.TrackerIdField).GetInt32ValueOrDefault(),

            ForumId = doc.GetField(Post.ForumIdField).GetInt32ValueOrDefault(),
            ForumName = doc.Get(Post.ForumNameField),

            Index = index
        };
    }

    //public Document Map(Post post)
    //{
    //    ArgumentNullException.ThrowIfNull(post);

    //    return new Document
    //    {
    //        new Int32Field(Post.IdField, post.Id, Field.Store.YES),

    //        new StringField(Post.CreatedField, DateTools.DateToString(post.Created, AppConsts.DefaultDateResolution), Field.Store.YES),
    //        new Int64Field(Post.SizeField, post.Size, Field.Store.YES),

    //        new TextField(Post.TitleField, post.Title, Field.Store.YES),
    //        new TextField(Post.ContentField, post.Content, Field.Store.YES),
    //        //new StoredField(Post.ContentField, ""),

    //        new StoredField(Post.HashField, post.Hash),

    //        new StoredField(Post.TrackerIdField, post.TrackerId),

    //        new Int32Field(Post.ForumIdField, post.ForumId, Field.Store.YES),
    //        new StringField(Post.ForumNameField, post.ForumName, Field.Store.YES)
    //    };
    //}

}
