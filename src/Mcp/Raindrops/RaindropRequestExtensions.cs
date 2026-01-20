using Mcp.Common;

namespace Mcp.Raindrops;

/// <summary>
/// Extension methods for converting request objects to <see cref="Raindrop"/> instances.
/// </summary>
internal static class RaindropRequestExtensions
{
    public static Raindrop ToRaindrop(this IRaindropRequest request)
    {
        return new Raindrop
        {
            Link = request.Link,
            Title = request.Title,
            Excerpt = request.Excerpt,
            Note = request.Note,
            Tags = request.Tags?.ToList(),
            Important = request.Important,
            Collection = request.CollectionId.HasValue
                ? new IdRef { Id = request.CollectionId.Value }
                : null
        };
    }
}
