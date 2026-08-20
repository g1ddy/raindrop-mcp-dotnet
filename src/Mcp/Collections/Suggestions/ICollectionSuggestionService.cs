using Mcp.Raindrops;

namespace Mcp.Collections.Suggestions;

public interface ICollectionSuggestionService
{
    Task<IReadOnlyList<CollectionSuggestion>> SuggestAsync(
        Raindrop bookmark,
        IReadOnlyCollection<Collection> collections,
        int limit,
        CancellationToken cancellationToken);

    void Invalidate();
}

public sealed record CollectionSuggestion(Collection Collection, double Score);
