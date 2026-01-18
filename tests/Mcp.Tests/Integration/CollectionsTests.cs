using System.Collections.Generic;
using Mcp.Collections;
using Mcp.Common;
using Mcp.Raindrops;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class CollectionsTests : TestBase
{
    public CollectionsTests() : base(s => s.AddTransient<CollectionsTools>()) { }

    [SkippableFact]
    public async Task Crud()
    {
        RequireApi();
        var collections = Provider.GetRequiredService<CollectionsTools>();
        var createResponse = await collections.CreateCollectionAsync(new Collection { Title = "Collections Crud - Create" }, CancellationToken.None);
        int collectionId = createResponse.Item.Id;
        try
        {
            await collections.UpdateCollectionAsync(collectionId, new Collection { Title = "Collections Crud - Updated" }, CancellationToken.None);
            var list = await collections.ListCollectionsAsync(CancellationToken.None);
            Assert.Contains(list.Items, c => c.Id == collectionId);
            var retrieved = await collections.GetCollectionAsync(collectionId, CancellationToken.None);
            Assert.Equal("Collections Crud - Updated", retrieved.Item.Title);
        }
        finally
        {
            await collections.DeleteCollectionAsync(collectionId, CancellationToken.None);
        }
    }

    [SkippableFact]
    public async Task ListChildren()
    {
        RequireApi();
        var collections = Provider.GetRequiredService<CollectionsTools>();
        int parentCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = "Collections ListChildren - Parent" }, CancellationToken.None)).Item.Id;
        int childCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = "Collections ListChildren - Child", Parent = new IdRef { Id = parentCollectionId } }, CancellationToken.None)).Item.Id;
        try
        {
            var result = await collections.ListChildCollectionsAsync(CancellationToken.None);
            Assert.Contains(result.Items, c => c.Id == childCollectionId);
        }
        finally
        {
            await collections.DeleteCollectionAsync(childCollectionId, CancellationToken.None);
            await collections.DeleteCollectionAsync(parentCollectionId, CancellationToken.None);
            var finalList = await collections.ListCollectionsAsync(CancellationToken.None);
            Assert.DoesNotContain(finalList.Items, c => c.Id == parentCollectionId);
        }
    }

    [SkippableFact]
    public async Task MergeCollections()
    {
        RequireApi();
        var collections = Provider.GetRequiredService<CollectionsTools>();
        int destinationId = (await collections.CreateCollectionAsync(new Collection { Title = "Collections Merge - Destination" }, CancellationToken.None)).Item.Id;
        int sourceId1 = (await collections.CreateCollectionAsync(new Collection { Title = "Collections Merge - Source1" }, CancellationToken.None)).Item.Id;
        int sourceId2 = (await collections.CreateCollectionAsync(new Collection { Title = "Collections Merge - Source2" }, CancellationToken.None)).Item.Id;

        try
        {
            var result = await collections.MergeCollectionsAsync(destinationId, new List<int> { sourceId1, sourceId2 }, CancellationToken.None);
            Assert.True(result.Result);

            var list = await collections.ListCollectionsAsync(CancellationToken.None);
            Assert.DoesNotContain(list.Items, c => c.Id == sourceId1);
            Assert.DoesNotContain(list.Items, c => c.Id == sourceId2);
        }
        finally
        {
            await collections.DeleteCollectionAsync(destinationId, CancellationToken.None);
        }
    }
}
