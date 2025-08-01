namespace Mcp.Raindrops;

/// <summary>
/// Extension methods for converting request objects to <see cref="Raindrop"/> instances.
/// </summary>
internal static class RaindropRequestExtensions
{
    public static Raindrop ToRaindrop(this RaindropCreateRequest request)
    {
        return new Raindrop
        {
            Link = request.Link,
            Title = request.Title,
            Excerpt = request.Excerpt,
            Note = request.Note,
            Tags = request.Tags?.ToList(),
            Important = request.Important,
            CollectionId = request.CollectionId
        };
    }

    public static Raindrop ToRaindrop(this RaindropUpdateRequest request)
    {
        return new Raindrop
        {
            Link = request.Link,
            Title = request.Title,
            Excerpt = request.Excerpt,
            Note = request.Note,
            Tags = request.Tags?.ToList(),
            Important = request.Important,
            CollectionId = request.CollectionId
        };
    }
}
