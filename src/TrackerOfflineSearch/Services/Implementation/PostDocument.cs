using Lucene.Net.Documents;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.Services.Implementation;

public class PostDocument
{
    private readonly Document _document;

    private readonly Int32Field _idField;
    private readonly Int64Field _createdField;
    private readonly NumericDocValuesField _createdSortField;
    private readonly Int64Field _sizeField;
    private readonly TextField _titleField;
    private readonly TextField _contentField;
    private readonly StoredField _hashField;
    private readonly StoredField _trackerIdField;
    private readonly Int32Field _forumIdField;
    private readonly StringField _forumNameField;

    public PostDocument()
    {
        _idField = new Int32Field(Post.IdField, 0, Field.Store.YES);
        _createdField = new Int64Field(Post.CreatedField, 0, Field.Store.YES);
        _createdSortField = new NumericDocValuesField(Post.CreatedSortField, 0);
        _sizeField = new Int64Field(Post.SizeField, 0, Field.Store.YES);
        _titleField = new TextField(Post.TitleField, "", Field.Store.YES);
        _contentField = new TextField(Post.ContentField, "", Field.Store.YES);
        _hashField = new StoredField(Post.HashField, "");
        _trackerIdField = new StoredField(Post.TrackerIdField, 0);
        _forumIdField = new Int32Field(Post.ForumIdField, 0, Field.Store.YES);
        _forumNameField = new StringField(Post.ForumNameField, "", Field.Store.YES);

        _document = new()
        {
            _idField,
            _createdField,
            _createdSortField,
            _sizeField,
            _titleField,
            _contentField,
            _hashField,
            _trackerIdField,
            _forumIdField,
            _forumNameField,
        };
    }

    public Document UpdateFrom(Post post)
    {
        _idField.SetInt32Value(post.Id);
        _createdField.SetInt64Value(post.Created.Ticks);
        _createdSortField.SetInt64Value(post.Created.Ticks);
        _sizeField.SetInt64Value(post.Size);
        _titleField.SetStringValue(post.Title);
        _contentField.SetStringValue(post.Content);
        _hashField.SetStringValue(post.Hash);
        _trackerIdField.SetInt32Value(post.TrackerId);
        _forumIdField.SetInt32Value(post.ForumId);
        _forumNameField.SetStringValue(post.ForumName);

        return _document;
    }
}
