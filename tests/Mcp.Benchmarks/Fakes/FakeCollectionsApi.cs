using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Collections;
using Mcp.Common;
using Refit;

namespace RaindropMcp.Benchmarks.Fakes;

public sealed class FakeCollectionsApi : ICollectionsApi
{
    private readonly IReadOnlyList<Collection> _collections;
    public int CallCount { get; private set; }

    public FakeCollectionsApi(int collectionCount)
    {
        _collections = GenerateCollections(collectionCount);
    }

    private static IReadOnlyList<Collection> GenerateCollections(int count)
    {
        var list = new List<Collection>(count);
        for (int i = 1; i <= count; i++)
        {
            var parentId = (i > 1 && i % 3 != 0) ? (int?)(i / 3 + 1) : null;
            if (parentId >= i) parentId = null;

            list.Add(new Collection
            {
                Id = i,
                Title = $"Collection {i}",
                Count = 0,
                Parent = parentId.HasValue ? new IdRef { Id = parentId.Value } : null,
                Access = new Access { For = 0, Level = 0, Root = false, Draggable = false },
                Color = null,
                Cover = null,
                Created = DateTime.UtcNow,
                CreatorRef = new UserRef { Id = 1 },
                Expanded = false,
                LastAction = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                Public = false,
                Sort = 0,
                View = "list"
            });
        }
        return list;
    }

    public Task<ItemsResponse<Collection>> ListAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new ItemsResponse<Collection>(true, _collections));
    }

    public Task<ItemsResponse<Collection>> ListChildrenAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new ItemsResponse<Collection>(true, Array.Empty<Collection>()));
    }

    public Task<ItemResponse<Collection>> GetAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<ItemResponse<Collection>> CreateAsync(Collection collection, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<ItemResponse<Collection>> UpdateAsync(int id, Collection collection, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<SuccessResponse> DeleteAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<SuccessResponse> MergeAsync(CollectionsMergeRequest payload, CancellationToken cancellationToken) => throw new NotImplementedException();

}
