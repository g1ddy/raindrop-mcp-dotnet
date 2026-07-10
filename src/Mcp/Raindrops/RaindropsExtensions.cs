using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Common;

namespace Mcp.Raindrops;

/// <summary>
/// Extension methods for <see cref="IRaindropsApi"/>.
/// </summary>
public static class RaindropsExtensions
{
    /// <summary>
    /// Fetches all bookmarks created after a specific date, handling pagination automatically.
    /// </summary>
    /// <param name="api">The API client.</param>
    /// <param name="since">The cutoff date (exclusive).</param>
    /// <param name="collectionId">The collection ID (0 for all).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of bookmarks newer than the specified date, ordered from newest to oldest.</returns>
    public static async Task<IReadOnlyList<Raindrop>> GetNewestSinceAsync(
        this IRaindropsApi api,
        DateTime since,
        int collectionId = 0,
        CancellationToken cancellationToken = default)
    {
        var newBookmarks = new List<Raindrop>();
        int page = 0;
        const int perPage = 50;
        var sinceUtc = since.ToUniversalTime();

        while (true)
        {
            var response = await api.ListAsync(collectionId, null, "-created", page, perPage, true, cancellationToken);
            if (response?.Result != true || response.Items == null || !response.Items.Any())
                break;

            bool reachedSeen = false;
            foreach (var bookmark in response.Items)
            {
                if (!bookmark.Created.HasValue) continue;

                if (bookmark.Created.Value.ToUniversalTime() <= sinceUtc)
                {
                    reachedSeen = true;
                    break;
                }
                newBookmarks.Add(bookmark);
            }

            if (reachedSeen || response.Items.Count < perPage)
                break;

            page++;
        }

        return newBookmarks;
    }
}
