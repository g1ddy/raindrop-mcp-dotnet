using System.Collections.Concurrent;

namespace Mcp.Collections.Suggestions;

internal sealed class CollectionSuggestionIndexCache
{
    private readonly ConcurrentDictionary<string, Lazy<Task<CollectionSuggestionIndex>>> _indexes = [];

    public Task<CollectionSuggestionIndex> GetOrCreateAsync(
        string cacheKey,
        Func<Task<CollectionSuggestionIndex>> factory)
    {
        var lazy = _indexes.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<CollectionSuggestionIndex>>(factory, LazyThreadSafetyMode.ExecutionAndPublication));

        return AwaitAndRemoveFaultedAsync(cacheKey, lazy);
    }

    public void Invalidate(string cacheKey) => _indexes.TryRemove(cacheKey, out _);

    private async Task<CollectionSuggestionIndex> AwaitAndRemoveFaultedAsync(
        string cacheKey,
        Lazy<Task<CollectionSuggestionIndex>> lazy)
    {
        try
        {
            return await lazy.Value;
        }
        catch
        {
            _indexes.TryRemove(new KeyValuePair<string, Lazy<Task<CollectionSuggestionIndex>>>(cacheKey, lazy));
            throw;
        }
    }
}

internal sealed record CollectionSuggestionIndex(
    IReadOnlyDictionary<int, CollectionFeatures> Collections,
    IReadOnlyDictionary<string, int> DocumentFrequencies,
    int DocumentCount,
    IReadOnlyDictionary<string, int> DomainTotals);

internal sealed record CollectionFeatures(
    IReadOnlyDictionary<string, int> Terms,
    IReadOnlySet<string> Tags,
    IReadOnlyDictionary<string, int> Domains);
