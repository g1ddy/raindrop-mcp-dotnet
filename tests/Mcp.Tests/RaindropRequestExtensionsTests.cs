using Mcp.Raindrops;
using Xunit;

namespace Mcp.Tests;

public class RaindropRequestExtensionsTests
{
    [Fact]
    public void ToRaindrop_FromCreateRequest_MapsAllProperties()
    {
        // Arrange
        var request = new RaindropCreateRequest
        {
            Link = "https://example.com",
            Title = "Example Title",
            Excerpt = "An example excerpt",
            Note = "A sample note",
            Tags = new[] { "tag1", "tag2" },
            Important = true,
            CollectionId = 123
        };

        // Act
        var result = request.ToRaindrop();

        // Assert
        Assert.Equal(request.Link, result.Link);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Excerpt, result.Excerpt);
        Assert.Equal(request.Note, result.Note);
        Assert.Equal(request.Tags, result.Tags);
        Assert.Equal(request.Important, result.Important);
        Assert.Equal(request.CollectionId, result.CollectionId);
    }

    [Fact]
    public void ToRaindrop_FromUpdateRequest_MapsAllProperties()
    {
        // Arrange
        var request = new RaindropUpdateRequest
        {
            Link = "https://example.com/updated",
            Title = "Updated Title",
            Excerpt = "Updated excerpt",
            Note = "Updated note",
            Tags = new[] { "tagA", "tagB" },
            Important = false,
            CollectionId = 456
        };

        // Act
        var result = request.ToRaindrop();

        // Assert
        Assert.Equal(request.Link, result.Link);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Excerpt, result.Excerpt);
        Assert.Equal(request.Note, result.Note);
        Assert.Equal(request.Tags, result.Tags);
        Assert.Equal(request.Important, result.Important);
        Assert.Equal(request.CollectionId, result.CollectionId);
    }

    [Fact]
    public void ToRaindrop_HandlesNullProperties()
    {
        // Arrange
        var request = new RaindropUpdateRequest
        {
            Link = null,
            Title = null,
            Excerpt = null,
            Note = null,
            Tags = null,
            Important = null,
            CollectionId = null
        };

        // Act
        var result = request.ToRaindrop();

        // Assert
        Assert.Null(result.Link);
        Assert.Null(result.Title);
        Assert.Null(result.Excerpt);
        Assert.Null(result.Note);
        Assert.Null(result.Tags);
        Assert.Null(result.Important);
        Assert.Null(result.CollectionId);
    }
}
