using System.ComponentModel;
using ModelContextProtocol.Server;
using Mcp.Common;
using Mcp.Tags;
using Mcp.Collections;

using Microsoft.Extensions.Options;

namespace Mcp.Raindrops;

[McpServerToolType]
public class RaindropsTools(IRaindropsApi api, RaindropCacheService cacheService, IOptions<RaindropOptions> options) :
    RaindropToolBase<IRaindropsApi>(api)
{
    private readonly RaindropCacheService _cacheService = cacheService;
    private readonly string _cacheKey = options.Value.ApiToken;
    private static readonly HashSet<string> ValidSortOptions = new(
        new[] { "created", "-created", "title", "-title", "domain", "-domain", "sort", "score" }
    );

    [McpServerTool(Title = "Create Bookmark"),
         Description("Creates a new bookmark.")]
    public async Task<ItemResponse<Raindrop>> CreateBookmarkAsync(
            [Description("Bookmark creation details")] RaindropCreateRequest request, CancellationToken cancellationToken)
    {
        var payload = request.ToRaindrop();
        var response = await Api.CreateAsync(payload, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateAll(_cacheKey);
        }
        return response;
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
            Title = "Get Bookmark"),
         Description("Retrieves a single bookmark by its unique ID.")]
    public Task<ItemResponse<Raindrop>> GetBookmarkAsync([
            Description("ID of the bookmark to retrieve")] long id, CancellationToken cancellationToken)
            => Api.GetAsync(id, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Update Bookmark"),
         Description("Updates an existing bookmark.")]
    public async Task<ItemResponse<Raindrop>> UpdateBookmarkAsync(
            [Description("ID of the bookmark to update")] long id,
            [Description("Updated bookmark data")] RaindropUpdateRequest request, CancellationToken cancellationToken)
    {
        var payload = request.ToRaindrop();
        var response = await Api.UpdateAsync(id, payload, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateAll(_cacheKey);
        }
        return response;
    }

    [McpServerTool(Idempotent = true, Title = "Delete Bookmark"),
         Description("Moves a bookmark to the Trash.")]
    public async Task<SuccessResponse> DeleteBookmarkAsync([
            Description("ID of the bookmark to delete")] long id, CancellationToken cancellationToken)
    {
        var response = await Api.DeleteAsync(id, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateAll(_cacheKey);
        }
        return response;
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
            Title = "List Bookmarks"),
         Description("Retrieves a list of bookmarks from a specific collection. For large collections, use pagination with perPage=50 to retrieve all bookmarks.")]
    public Task<ItemsResponse<Raindrop>> ListBookmarksAsync(
            [Description("The ID of the collection to retrieve bookmarks from. Use 0 for all, -1 for unsorted, -99 for trash.")] int collectionId,
            [Description(SearchSyntax.Description)] string? search = null,
            [Description("Sorting order: '-created' (newest, default), 'created', 'score' (relevance when searching), 'sort', 'title', '-title', 'domain', '-domain'.")] string? sort = null,
            [Description("Page index starting from 0.")] int? page = null,
            [Description("How many raindrops per page, up to 50.")] int? perPage = null,
            [Description("Include bookmarks from nested collections (true/false).")] bool? nested = null,
            CancellationToken cancellationToken = default)
    {
        if (page is < 0)
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page number cannot be negative.");

        if (perPage is > 50 or < 1)
            throw new ArgumentOutOfRangeException(nameof(perPage), perPage, "Number of items per page must be between 1 and 50.");

        if (sort is not null)
        {
            if (!ValidSortOptions.Contains(sort))
                throw new ArgumentOutOfRangeException(nameof(sort), sort, $"Valid values are '{string.Join("', '", ValidSortOptions)}'.");

            if (sort == "score" && string.IsNullOrWhiteSpace(search))
                throw new ArgumentException("Sort 'score' is only allowed when using a search query.", nameof(sort));
        }

        return Api.ListAsync(collectionId, search, sort, page, perPage, nested, cancellationToken);
    }

    [McpServerTool(Title = "Create Bookmarks"),
         Description("Creates multiple bookmarks in a single request.")]
    public async Task<ItemsResponse<Raindrop>> CreateBookmarksAsync(
            [Description("Collection ID for the new bookmarks")] int collectionId,
            [Description("A collection of bookmark details to create.")] IReadOnlyList<Raindrop> raindrops,
            CancellationToken cancellationToken = default)
    {
        const int ChunkSize = 100;
        int count = raindrops.Count;
        var allItems = new List<Raindrop>(count);
        var overallResult = true;

        for (int i = 0; i < count; i += ChunkSize)
        {
            int currentChunkSize = Math.Min(ChunkSize, count - i);
            var chunk = new ReadOnlyListSlice<Raindrop>(raindrops, i, currentChunkSize);

            var payload = new RaindropCreateManyRequest
            {
                CollectionId = collectionId,
                Items = chunk
            };

            var response = await Api.CreateManyAsync(payload, cancellationToken);

            if (response.Result)
            {
                if (response.Items is not null)
                {
                    allItems.AddRange(response.Items);
                }
            }
            else
            {
                overallResult = false;
                break;
            }
        }

        if (allItems.Count > 0)
        {
            _cacheService.InvalidateAll(_cacheKey);
        }

        return new ItemsResponse<Raindrop>(overallResult, allItems);
    }

    /// <summary>
    /// A zero-allocation slice of an IReadOnlyList.
    /// Implements IList to allow System.Text.Json to serialize without allocating enumerators.
    /// </summary>
    private sealed class ReadOnlyListSlice<T> : IReadOnlyList<T>, IList<T>
    {
        private readonly IReadOnlyList<T> _source;
        private readonly int _offset;
        private readonly int _count;

        public ReadOnlyListSlice(IReadOnlyList<T> source, int offset, int count)
        {
            _source = source;
            _offset = offset;
            _count = count;
        }

        public T this[int index]
        {
            get => index >= 0 && index < _count ? _source[_offset + index] : throw new ArgumentOutOfRangeException(nameof(index));
            set => throw new NotSupportedException();
        }

        public int Count => _count;
        public bool IsReadOnly => true;

        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _count; i++) array[arrayIndex + i] = _source[_offset + i];
        }
        public int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
                if (EqualityComparer<T>.Default.Equals(_source[_offset + i], item)) return i;
            return -1;
        }
        public void Insert(int index, T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++) yield return _source[_offset + i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [McpServerTool(Idempotent = true, Title = "Update Bookmarks"),
     Description("Bulk update bookmarks in a collection. For precise targeting, use the ids parameter in the update object.")]
    public async Task<SuccessResponse> UpdateBookmarksAsync(
        [Description("Collection to update")] int collectionId,
        [Description("Update operations to apply")] RaindropBulkUpdate update,
        [Description("Apply to nested collections")] bool? nested = null,
        [Description("Optional search filter. Use cautiously as it may affect more bookmarks than intended.")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Api.UpdateManyAsync(collectionId, update, nested, search, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateAll(_cacheKey);
        }
        return response;
    }
}
