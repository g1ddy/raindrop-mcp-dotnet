using System.Text.RegularExpressions;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;

namespace Mcp.Collections.Suggestions;

internal sealed partial class CollectionSuggestionService(
    IRaindropsApi raindropsApi,
    CollectionSuggestionIndexCache cache,
    IOptions<RaindropOptions> options) : ICollectionSuggestionService
{
    private const int PageSize = 50;
    private const double LexicalWeight = 0.60;
    private const double TagWeight = 0.20;
    private const double DomainWeight = 0.20;
    private static readonly HashSet<string> _stopWords = new(StringComparer.Ordinal)
    {
        "http", "https", "www"
    };
    private readonly string _cacheKey = options.Value.ApiToken;

    public async Task<IReadOnlyList<CollectionSuggestion>> SuggestAsync(
        Raindrop bookmark,
        IReadOnlyCollection<Collection> collections,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        ArgumentNullException.ThrowIfNull(collections);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var candidates = collections
            .Where(collection => collection.Id > 0 && !string.IsNullOrWhiteSpace(collection.Title))
            .ToDictionary(collection => collection.Id);
        if (candidates.Count == 0)
            return [];

        var index = await cache.GetOrCreateAsync(
            _cacheKey,
            () => BuildIndexAsync(candidates, cancellationToken));
        var queryTerms = TokenizeBookmark(bookmark);
        var queryTags = NormalizeTags(bookmark.Tags);
        var queryDomain = Normalize(bookmark.Domain);

        return candidates.Values
            .Select(collection => new CollectionSuggestion(
                collection,
                Score(collection.Id, queryTerms, queryTags, queryDomain, index)))
            .Where(suggestion => suggestion.Score > 0)
            .OrderByDescending(suggestion => suggestion.Score)
            .ThenBy(suggestion => suggestion.Collection.Id)
            .Take(limit)
            .ToList();
    }

    public void Invalidate() => cache.Invalidate(_cacheKey);

    private async Task<CollectionSuggestionIndex> BuildIndexAsync(
        IReadOnlyDictionary<int, Collection> candidates,
        CancellationToken cancellationToken)
    {
        var terms = candidates.ToDictionary(
            pair => pair.Key,
            pair => CountTerms(Tokenize(pair.Value.Title, pair.Value.Description)));
        var tags = candidates.Keys.ToDictionary(id => id, _ => new HashSet<string>(StringComparer.Ordinal));
        var domains = candidates.Keys.ToDictionary(id => id, _ => new Dictionary<string, int>(StringComparer.Ordinal));
        var domainTotals = new Dictionary<string, int>(StringComparer.Ordinal);
        var seenBookmarkIds = new HashSet<long>();

        for (var page = 0; ; page++)
        {
            var response = await raindropsApi.ListAsync(0, null, null, page, PageSize, true, cancellationToken);
            if (response.Result != true || response.Items is null)
                throw new InvalidOperationException($"Raindrop did not return bookmark page {page} while building the suggestion index.");

            var newBookmarks = 0;
            foreach (var bookmark in response.Items)
            {
                if (!seenBookmarkIds.Add(bookmark.Id))
                    continue;
                newBookmarks++;

                if (bookmark.Collection is not { Id: var collectionId } || !candidates.ContainsKey(collectionId))
                    continue;

                AddTermCounts(terms[collectionId], TokenizeBookmark(bookmark));
                tags[collectionId].UnionWith(NormalizeTags(bookmark.Tags));

                var domain = Normalize(bookmark.Domain);
                if (domain is null)
                    continue;
                Increment(domains[collectionId], domain);
                Increment(domainTotals, domain);
            }

            if (response.Items.Count < PageSize || newBookmarks == 0)
                break;
        }

        var documentFrequencies = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var documentTerms in terms.Values)
            foreach (var term in documentTerms.Keys)
                Increment(documentFrequencies, term);

        var features = candidates.Keys.ToDictionary(
            id => id,
            id => new CollectionFeatures(terms[id], tags[id], domains[id]));
        return new CollectionSuggestionIndex(features, documentFrequencies, candidates.Count, domainTotals);
    }

    private static double Score(
        int collectionId,
        IReadOnlyDictionary<string, int> queryTerms,
        IReadOnlySet<string> queryTags,
        string? queryDomain,
        CollectionSuggestionIndex index)
    {
        if (!index.Collections.TryGetValue(collectionId, out var features))
            return 0;

        var lexical = TfIdfCosine(queryTerms, features.Terms, index.DocumentFrequencies, index.DocumentCount);
        var tag = Jaccard(queryTags, features.Tags);
        var domain = queryDomain is not null && index.DomainTotals.TryGetValue(queryDomain, out var total)
            ? (double)features.Domains.GetValueOrDefault(queryDomain) / total
            : 0;
        return LexicalWeight * lexical + TagWeight * tag + DomainWeight * domain;
    }

    private static double TfIdfCosine(
        IReadOnlyDictionary<string, int> left,
        IReadOnlyDictionary<string, int> right,
        IReadOnlyDictionary<string, int> documentFrequencies,
        int documentCount)
    {
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        foreach (var (term, count) in left)
        {
            var weight = count * InverseDocumentFrequency(term, documentFrequencies, documentCount);
            leftMagnitude += weight * weight;
            if (right.TryGetValue(term, out var rightCount))
                dot += weight * rightCount * InverseDocumentFrequency(term, documentFrequencies, documentCount);
        }

        foreach (var (term, count) in right)
        {
            var weight = count * InverseDocumentFrequency(term, documentFrequencies, documentCount);
            rightMagnitude += weight * weight;
        }

        return leftMagnitude == 0 || rightMagnitude == 0 ? 0 : dot / Math.Sqrt(leftMagnitude * rightMagnitude);
    }

    private static double InverseDocumentFrequency(
        string term,
        IReadOnlyDictionary<string, int> documentFrequencies,
        int documentCount) =>
        Math.Log((documentCount + 1d) / (documentFrequencies.GetValueOrDefault(term) + 1d)) + 1d;

    private static double Jaccard(IReadOnlySet<string> left, IReadOnlySet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0;
        var intersection = left.Count(right.Contains);
        return (double)intersection / (left.Count + right.Count - intersection);
    }

    private static Dictionary<string, int> TokenizeBookmark(Raindrop bookmark) =>
        CountTerms(Tokenize(bookmark.Title, bookmark.Link, bookmark.Excerpt, bookmark.Note, bookmark.Type, bookmark.Domain)
            .Concat(NormalizeTags(bookmark.Tags)));

    private static IEnumerable<string> Tokenize(params string?[] values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => TokenRegex().Matches(value!).Select(match => match.Value.ToLowerInvariant()))
            .Where(term => term.Length > 1 && !_stopWords.Contains(term));

    private static HashSet<string> NormalizeTags(IEnumerable<string>? values) =>
        values?.Select(Normalize).OfType<string>().ToHashSet(StringComparer.Ordinal) ?? [];

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static Dictionary<string, int> CountTerms(IEnumerable<string> values)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        AddTerms(result, values);
        return result;
    }

    private static void AddTerms(Dictionary<string, int> terms, IEnumerable<string> values)
    {
        foreach (var value in values)
            Increment(terms, value);
    }

    private static void AddTermCounts(Dictionary<string, int> terms, IReadOnlyDictionary<string, int> values)
    {
        foreach (var (term, count) in values)
            terms[term] = terms.GetValueOrDefault(term) + count;
    }

    private static void Increment(Dictionary<string, int> values, string key) =>
        values[key] = values.GetValueOrDefault(key) + 1;

    [GeneratedRegex(@"[\p{L}\p{Nd}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}
