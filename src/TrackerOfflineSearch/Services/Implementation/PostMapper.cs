using System;
using System.Xml.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using TrackerOfflineSearch.Core.Models;

namespace TrackerOfflineSearch.Services.Implementation;

public class PostMapper : IPostMapper
{
    #region IPostMapper implementation

    public Post ToDomain(Document doc)
    {
        if (doc is null)
            throw new ArgumentNullException(nameof(doc));

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
            ForumName = doc.Get(Post.ForumNameField)
        };
    }

    public Post ToDomain(XElement el)
    {
        if (el is null)
            throw new ArgumentNullException(nameof(el));

        var id = (int)el.Attribute("id");
        var created = DateTime.SpecifyKind(DateTime.ParseExact(el.Attribute("registred_at").Value, "yyyy.MM.d H:m:s", null), DateTimeKind.Utc);
        var size = (long)el.Attribute("size");

        var title = el.Element("title").Value;

        var torrent = el.Element("torrent");
        var hash = torrent.Attribute("hash").Value;
        var trackerId = (int)torrent.Attribute("tracker_id");

        var forum = el.Element("forum");
        var forumId = (int)forum.Attribute("id");
        var forumName = forum.Value;

        var content = el.Element("content").Value;

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

    public Document ToRepository(Post post)
    {
        if (post is null)
            throw new ArgumentNullException(nameof(post));

        var doc = new Document
        {
            new Int32Field(Post.IdField, post.Id, Field.Store.YES),

            new StringField(Post.CreatedField, DateTools.DateToString(post.Created, AppConst.DefaultDateResolution), Field.Store.YES),
            new Int64Field(Post.SizeField, post.Size, Field.Store.YES),

            new TextField(Post.TitleField, post.Title, Field.Store.YES),
            new TextField(Post.ContentField, post.Content, Field.Store.YES),

            new StoredField(Post.HashField, post.Hash),

            new StoredField(Post.TrackerIdField, post.TrackerId),

            new Int32Field(Post.ForumIdField, post.ForumId, Field.Store.YES),
            new StringField(Post.ForumNameField, post.ForumName, Field.Store.YES)
        };
        return doc;
    }

    #endregion
}
