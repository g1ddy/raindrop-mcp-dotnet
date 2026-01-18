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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cts.Token;

        var collections = Provider.GetRequiredService<CollectionsTools>();
        var createResponse = await collections.CreateCollectionAsync(new Collection { Title = "Collections Crud - Create" }, cancellationToken);
        int collectionId = createResponse.Item.Id;
        try
        {
            await collections.UpdateCollectionAsync(collectionId, new Collection { Title = "Collections Crud - Updated" }, cancellationToken);
            var list = await collections.ListCollectionsAsync(cancellationToken);
            Assert.Contains(list.Items, c => c.Id == collectionId);
            var retrieved = await collections.GetCollectionAsync(collectionId, cancellationToken);
            Assert.Equal("Collections Crud - Updated", retrieved.Item.Title);
        }
        finally
        {
            await collections.DeleteCollectionAsync(collectionId, cancellationToken);
        }
    }

    [SkippableFact]
    public async Task ListChildren()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cts.Token;

        var collections = Provider.GetRequiredService<CollectionsTools>();
        int parentCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = "Collections ListChildren - Parent" }, cancellationToken)).Item.Id;
        int childCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = "Collections ListChildren - Child", Parent = new IdRef { Id = parentCollectionId } }, cancellationToken)).Item.Id;
        try
        {
            var result = await collections.ListChildCollectionsAsync(cancellationToken);
            Assert.Contains(result.Items, c => c.Id == childCollectionId);
        }
        finally
        {
            await collections.DeleteCollectionAsync(childCollectionId, cancellationToken);
            await collections.DeleteCollectionAsync(parentCollectionId, cancellationToken);
            var finalList = await collections.ListCollectionsAsync(cancellationToken);
            Assert.DoesNotContain(finalList.Items, c => c.Id == parentCollectionId);
            Assert.DoesNotContain(finalList.Items, c => c.Id == childCollectionId);
        }
    }

    [SkippableFact]
    public async Task MergeCollections()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cts.Token;

        var collections = Provider.GetRequiredService<CollectionsTools>();
        int destinationId = (await collections.CreateCollectionAsync(new Collection { Title = "Collections Merge - Destination" }, cancellationToken)).Item.Id;
        int sourceId1 = (await collections.CreateCollectionAsync(new Collection { Title = "Collections Merge - Source1" }, cancellationToken)).Item.Id;
        int sourceId2 = (await collections.CreateCollectionAsync(new Collection { Title = "Collections Merge - Source2" }, cancellationToken)).Item.Id;

        try
        {
            var result = await collections.MergeCollectionsAsync(destinationId, new List<int> { sourceId1, sourceId2 }, cancellationToken);
            Assert.True(result.Result);

            var list = await collections.ListCollectionsAsync(cancellationToken);
            Assert.DoesNotContain(list.Items, c => c.Id == sourceId1);
            Assert.DoesNotContain(list.Items, c => c.Id == sourceId2);
        }
        finally
        {
            await collections.DeleteCollectionAsync(destinationId, cancellationToken);
        }
    }
}
