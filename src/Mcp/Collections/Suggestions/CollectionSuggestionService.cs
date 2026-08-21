using System.Text.RegularExpressions;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;

namespace Mcp.Collections.Suggestions;

internal sealed partial class CollectionSuggestionService(
    IRaindropsApi raindropsApi,
    CollectionSuggestionIndexCache cache,
    IOptions<RaindropOptions> options,
    Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>? embeddingGenerator = null) : ICollectionSuggestionService
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

        var queryTerms = TokenizeBookmark(bookmark);
        var queryTags = NormalizeTags(bookmark.Tags);
        var queryDomain = Normalize(bookmark.Domain);
        var canonicalString = CreateCanonicalString(bookmark);

        Microsoft.Extensions.AI.Embedding<float>? queryEmbedding = null;
        if (embeddingGenerator != null && !string.IsNullOrWhiteSpace(canonicalString))
        {
            try
            {
                var embeddings = await embeddingGenerator.GenerateAsync([canonicalString], null, cancellationToken);
                queryEmbedding = embeddings.FirstOrDefault();
            }
            catch
            {
                // Fallback to Phase 1 lexical logic if embedding generation fails
                queryEmbedding = null;
            }
        }

        var index = await cache.GetOrCreateAsync(
            _cacheKey,
            () => BuildIndexAsync(candidates, cancellationToken));

        index.Bookmarks.TryGetValue(bookmark.Id, out var indexedBookmark);

        return candidates.Values
            .Select(collection => new CollectionSuggestion(
                collection,
                Score(collection.Id, queryTerms, queryTags, queryDomain, queryEmbedding, index, indexedBookmark)))
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
        var tags = candidates.Keys.ToDictionary(id => id, _ => new Dictionary<string, int>(StringComparer.Ordinal));
        var domains = candidates.Keys.ToDictionary(id => id, _ => new Dictionary<string, int>(StringComparer.Ordinal));
        var domainTotals = new Dictionary<string, int>(StringComparer.Ordinal);
        var seenBookmarkIds = new HashSet<long>();
        var indexedBookmarks = new Dictionary<long, IndexedBookmark>();

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

                var bookmarkTerms = TokenizeBookmark(bookmark);
                var canonicalString = CreateCanonicalString(bookmark);
                var bookmarkTags = NormalizeTags(bookmark.Tags);
                var bookmarkDomain = Normalize(bookmark.Domain);

                AddTermCounts(terms[collectionId], bookmarkTerms);
                foreach (var tag in bookmarkTags)
                {
                    Increment(tags[collectionId], tag);
                }

                if (bookmarkDomain is not null)
                {
                    Increment(domains[collectionId], bookmarkDomain);
                    Increment(domainTotals, bookmarkDomain);
                }

                if (bookmark.Id != 0)
                {
                    indexedBookmarks[bookmark.Id] = new IndexedBookmark(
                        bookmark.Id,
                        collectionId,
                        bookmarkTerms,
                        bookmarkTags,
                        bookmarkDomain,
                        canonicalString);
                }
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

        if (embeddingGenerator != null)
        {
            var bookmarksList = indexedBookmarks.Values.ToList();
            var canonicalStrings = bookmarksList.Select(b => b.CanonicalString).ToList();
            if (canonicalStrings.Count > 0)
            {
                try
                {
                    var embeddings = await embeddingGenerator.GenerateAsync(canonicalStrings, null, cancellationToken);
                    var vectors = embeddings.Select(e => e.Vector.ToArray()).ToList();

                    if (vectors.Count == bookmarksList.Count)
                    {
                        var collectionVectors = new Dictionary<int, List<float[]>>();
                        for (int i = 0; i < bookmarksList.Count; i++)
                        {
                            var b = bookmarksList[i];
                            if (!collectionVectors.TryGetValue(b.CollectionId, out var list))
                            {
                                list = new List<float[]>();
                                collectionVectors[b.CollectionId] = list;
                            }
                            list.Add(vectors[i]);
                        }

                        foreach (var kvp in collectionVectors)
                        {
                            if (features.TryGetValue(kvp.Key, out var oldFeatures))
                            {
                                var centroid = CalculateCentroid(kvp.Value);
                                features[kvp.Key] = oldFeatures with { Centroid = new Microsoft.Extensions.AI.Embedding<float>(centroid) };
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to Phase 1: skip generating centroids if embedding generation fails
                }
            }
        }

        return new CollectionSuggestionIndex(features, documentFrequencies, candidates.Count, domainTotals, indexedBookmarks);
    }

    private static double Score(
        int collectionId,
        IReadOnlyDictionary<string, int> queryTerms,
        IReadOnlySet<string> queryTags,
        string? queryDomain,
        Microsoft.Extensions.AI.Embedding<float>? queryEmbedding,
        CollectionSuggestionIndex index,
        IndexedBookmark? indexedBookmark)
    {
        if (!index.Collections.TryGetValue(collectionId, out var features))
            return 0;

        var isTargetCollection = indexedBookmark != null && collectionId == indexedBookmark.CollectionId;

        var lexical = TfIdfCosine(queryTerms, collectionId, features, index, indexedBookmark);
        var tag = Jaccard(queryTags, features.Tags, isTargetCollection ? indexedBookmark : null);

        double domain = 0;
        if (queryDomain is not null && index.DomainTotals.TryGetValue(queryDomain, out var total))
        {
            var effectiveTotal = queryDomain == indexedBookmark?.Domain ? total - 1 : total;
            if (effectiveTotal > 0)
            {
                var domainCount = features.Domains.GetValueOrDefault(queryDomain);
                var effectiveDomainCount = isTargetCollection && queryDomain == indexedBookmark?.Domain
                    ? domainCount - 1
                    : domainCount;
                domain = (double)effectiveDomainCount / effectiveTotal;
            }
        }


        double semantic = 0;
        double finalScore = 0;

        if (queryEmbedding != null && features.Centroid != null)
        {
            semantic = CosineSimilarity(queryEmbedding.Vector.ToArray(), features.Centroid.Vector.ToArray());
            // Blend Phase 2 Semantic with Phase 1 scores
            finalScore = 0.5 * semantic + 0.3 * lexical + 0.1 * tag + 0.1 * domain;
        }
        else
        {
            // Fallback to pure Phase 1
            finalScore = LexicalWeight * lexical + TagWeight * tag + DomainWeight * domain;
        }

        return finalScore;
    }


    private static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || right.Length == 0 || left.Length != right.Length)
            return 0;

        double dot = 0;
        double leftMag = 0;
        double rightMag = 0;

        for (int i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftMag += left[i] * left[i];
            rightMag += right[i] * right[i];
        }

        if (leftMag == 0 || rightMag == 0) return 0;

        return dot / (Math.Sqrt(leftMag) * Math.Sqrt(rightMag));
    }

    private static double TfIdfCosine(
        IReadOnlyDictionary<string, int> left,
        int collectionId,
        CollectionFeatures features,
        CollectionSuggestionIndex index,
        IndexedBookmark? indexedBookmark)
    {
        var isTargetCollection = indexedBookmark != null && collectionId == indexedBookmark.CollectionId;
        IReadOnlyDictionary<string, int>? targetTerms = indexedBookmark != null && index.Collections.TryGetValue(indexedBookmark.CollectionId, out var targetFeatures)
            ? targetFeatures.Terms
            : null;

        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;

        foreach (var (term, count) in left)
        {
            var docFreq = GetEffectiveDocFreq(term, targetTerms, indexedBookmark, index.DocumentFrequencies);
            var weight = count * InverseDocumentFrequency(term, docFreq, index.DocumentCount);
            leftMagnitude += weight * weight;

            var rightCount = GetEffectiveRightCount(term, isTargetCollection, features.Terms, indexedBookmark);
            if (rightCount > 0)
            {
                dot += weight * rightCount * InverseDocumentFrequency(term, docFreq, index.DocumentCount);
            }
        }

        foreach (var (term, _) in features.Terms)
        {
            var rightCount = GetEffectiveRightCount(term, isTargetCollection, features.Terms, indexedBookmark);
            if (rightCount > 0)
            {
                var docFreq = GetEffectiveDocFreq(term, targetTerms, indexedBookmark, index.DocumentFrequencies);
                var weight = rightCount * InverseDocumentFrequency(term, docFreq, index.DocumentCount);
                rightMagnitude += weight * weight;
            }
        }

        return leftMagnitude == 0 || rightMagnitude == 0 ? 0 : dot / Math.Sqrt(leftMagnitude * rightMagnitude);
    }

    private static int GetEffectiveRightCount(
        string term,
        bool isTargetCollection,
        IReadOnlyDictionary<string, int> terms,
        IndexedBookmark? indexedBookmark)
    {
        var count = terms.GetValueOrDefault(term);
        if (isTargetCollection && indexedBookmark != null)
        {
            count -= indexedBookmark.Terms.GetValueOrDefault(term);
        }
        return Math.Max(0, count);
    }

    private static int GetEffectiveDocFreq(
        string term,
        IReadOnlyDictionary<string, int>? targetTerms,
        IndexedBookmark? indexedBookmark,
        IReadOnlyDictionary<string, int> documentFrequencies)
    {
        var docFreq = documentFrequencies.GetValueOrDefault(term);
        if (indexedBookmark != null && targetTerms != null)
        {
            var totalInTarget = targetTerms.GetValueOrDefault(term);
            var inBookmark = indexedBookmark.Terms.GetValueOrDefault(term);
            if (totalInTarget > 0 && totalInTarget == inBookmark)
            {
                docFreq--;
            }
        }
        return Math.Max(0, docFreq);
    }

    private static double InverseDocumentFrequency(
        string term,
        int docFreq,
        int documentCount) =>
        Math.Log((documentCount + 1d) / (docFreq + 1d)) + 1d;

    private static double Jaccard(
        IReadOnlySet<string> left,
        IReadOnlyDictionary<string, int> right,
        IndexedBookmark? targetIndexedBookmark)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0;

        int effectiveRightCount = 0;
        int intersection = 0;

        if (targetIndexedBookmark is null)
        {
            effectiveRightCount = right.Count;
            intersection = left.Count(right.ContainsKey);
        }
        else
        {
            foreach (var (tag, count) in right)
            {
                var remainingCount = count - (targetIndexedBookmark.Tags.Contains(tag) ? 1 : 0);
                if (remainingCount > 0)
                {
                    effectiveRightCount++;
                    if (left.Contains(tag))
                    {
                        intersection++;
                    }
                }
            }
        }

        if (effectiveRightCount == 0)
            return 0;

        var union = left.Count + effectiveRightCount - intersection;
        return union <= 0 ? 0 : (double)intersection / union;
    }

        private static string CreateCanonicalString(Raindrop bookmark)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(bookmark.Title)) parts.Add(bookmark.Title);
        if (!string.IsNullOrWhiteSpace(bookmark.Excerpt)) parts.Add(bookmark.Excerpt);
        if (!string.IsNullOrWhiteSpace(bookmark.Note)) parts.Add(bookmark.Note);
        if (bookmark.Tags?.Any() == true) parts.Add($"Tags: {string.Join(", ", bookmark.Tags)}");
        if (!string.IsNullOrWhiteSpace(bookmark.Domain)) parts.Add($"Domain: {bookmark.Domain}");
        return string.Join("\n", parts);
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


    private static float[] CalculateCentroid(List<float[]> vectors)
    {
        if (vectors.Count == 0) return Array.Empty<float>();

        int dim = vectors[0].Length;
        var centroid = new float[dim];

        foreach (var v in vectors)
        {
            for (int i = 0; i < dim; i++)
            {
                centroid[i] += v[i];
            }
        }

        for (int i = 0; i < dim; i++)
        {
            centroid[i] /= vectors.Count;
        }

        double mag = 0;
        for (int i = 0; i < dim; i++) mag += centroid[i] * centroid[i];
        if (mag > 0)
        {
            mag = Math.Sqrt(mag);
            for (int i = 0; i < dim; i++) centroid[i] = (float)(centroid[i] / mag);
        }

        return centroid;
    }

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
