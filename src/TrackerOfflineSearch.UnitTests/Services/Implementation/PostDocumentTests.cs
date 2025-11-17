using Lucene.Net.Documents;
using TrackerOfflineSearch.Services.Implementation;
using TrackerOfflineSearch.Services.Models;

namespace TrackerOfflineSearch.UnitTests.Services.Implementation;

public class PostDocumentTests
{
    [Fact]
    public void UpdateFrom_ShouldFillAllFieldsCorrectly()
    {
        // Arrange
        var post = new Post
        {
            Id = 123,
            Created = new DateTime(2024, 1, 1, 10, 20, 30),
            Size = 555,
            Title = "Hello world",
            Content = "content text",
            Hash = "abc123",
            TrackerId = 77,
            ForumId = 10,
            ForumName = "test forum"
        };

        var postDoc = new PostDocument();

        // Act
        Document document = postDoc.UpdateFrom(post);

        // Assert
        Assert.NotNull(document);

        Assert.Equal("123", document.Get(Post.IdField));
        Assert.Equal(post.Created.Ticks.ToString(), document.Get(Post.CreatedField));

        // DocValues не хранятся в Get(), проверяем через GetField()
        var createdSortField = document.GetField(Post.CreatedSortField);
        Assert.NotNull(createdSortField);
        Assert.Equal(post.Created.Ticks, createdSortField.GetInt64Value());

        Assert.Equal(post.Size.ToString(), document.Get(Post.SizeField));
        Assert.Equal(post.Title, document.Get(Post.TitleField));
        Assert.Equal(post.Content, document.Get(Post.ContentField));
        Assert.Equal(post.Hash, document.Get(Post.HashField));
        Assert.Equal(post.TrackerId.ToString(), document.Get(Post.TrackerIdField));
        Assert.Equal(post.ForumId.ToString(), document.Get(Post.ForumIdField));
        Assert.Equal(post.ForumName, document.Get(Post.ForumNameField));
    }

    [Fact]
    public void UpdateFrom_ShouldReturnSameDocumentInstance()
    {
        // Arrange
        var post = new Post
        {
            Id = 1,
            Created = DateTime.UtcNow,
            Size = 10,
            Title = "t",
            Content = "c",
            Hash = "h",
            TrackerId = 2,
            ForumId = 3,
            ForumName = "f"
        };

        var postDoc = new PostDocument();

        // Act
        var doc1 = postDoc.UpdateFrom(post);
        var doc2 = postDoc.UpdateFrom(post);

        // Assert — всегда возвращает один и тот же Document
        Assert.Same(doc1, doc2);
    }
}
