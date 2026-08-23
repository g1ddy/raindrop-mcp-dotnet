using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Raindrops;
using Mcp.Common;
using Mcp.Collections;
using Refit;

namespace RaindropMcp.Benchmarks.Fakes;

public sealed class FakeRaindropsApi : IRaindropsApi
{
    private readonly IReadOnlyList<Raindrop> _raindrops;
    public int CallCount { get; private set; }

    public FakeRaindropsApi(int count, int collectionCount)
    {
        _raindrops = GenerateRaindrops(count, collectionCount);
    }

    private static IReadOnlyList<Raindrop> GenerateRaindrops(int count, int collectionCount)
    {
        var list = new List<Raindrop>(count);
        var domains = new[] { "example.com", "test.org", "raindrop.io", "github.com", "reddit.com" };
        var tags = new[] { "tag1", "tag2", "tag3", "tag4", "tag5" };

        for (int i = 1; i <= count; i++)
        {
            var collectionId = (i % collectionCount) + 1;
            var itemTags = new List<string>();
            if (i % 2 == 0) itemTags.Add(tags[i % tags.Length]);
            if (i % 3 == 0) itemTags.Add(tags[(i + 1) % tags.Length]);

            list.Add(new Raindrop
            {
                Id = i,
                Title = $"Bookmark {i}",
                Link = $"https://{domains[i % domains.Length]}/page/{i}",
                Domain = domains[i % domains.Length],
                Collection = new IdRef { Id = collectionId },
                Tags = itemTags,
                Excerpt = i % 4 == 0 ? null : "Sample excerpt text",
                Note = "",
                Type = "link",
                Cover = "",
                Created = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow
            });
        }
        return list;
    }

    public Task<ItemsResponse<Raindrop>> ListAsync(
        int collectionId,
        string? search,
        string? sort,
        int? page,
        int? perpage,
        bool? nested,
        CancellationToken cancellationToken)
    {
        CallCount++;

        IEnumerable<Raindrop> filtered = _raindrops;
        if (collectionId > 0)
        {
            filtered = filtered.Where(r => r.Collection?.Id == collectionId);
        }

        int pg = page ?? 0;
        int pp = perpage ?? 50;

        var paged = filtered.Skip(pg * pp).Take(pp).ToList();

        return Task.FromResult(new ItemsResponse<Raindrop>(true, paged));
    }

    public Task<ItemResponse<Raindrop>> GetAsync(long id, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<ItemResponse<Raindrop>> CreateAsync(Raindrop raindrop, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<ItemResponse<Raindrop>> UpdateAsync(long id, Raindrop raindrop, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<SuccessResponse> DeleteAsync(long id, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<ItemsResponse<Raindrop>> CreateManyAsync(RaindropCreateManyRequest payload, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<SuccessResponse> UpdateManyAsync(int collectionId, RaindropBulkUpdate update, bool? nested, string? search, CancellationToken cancellationToken) => throw new NotImplementedException();
}
