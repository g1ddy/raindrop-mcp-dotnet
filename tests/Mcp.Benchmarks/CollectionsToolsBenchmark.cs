using BenchmarkDotNet.Attributes;
using Mcp.Collections;
using Mcp.Collections.Suggestions;
using Mcp.Raindrops;
using Mcp.Common;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System;
using Microsoft.Extensions.Options;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class CollectionsToolsBenchmark : RaindropBenchmarkBase
{
    private CollectionsTools _tools = null!;
    private Mock<McpServer> _mcpServerMock = null!;
    private Mock<ICollectionsApi> _collectionsApiMock = null!;
    private Mock<IRaindropsApi> _raindropsApiMock = null!;
    private Mock<ICollectionSuggestionService> _suggestionServiceMock = null!;
    private List<Collection> _largeCollectionList = null!;

    [Params(100, 1000)]
    public int CollectionCount;

    public override void SetupCache()
    {
        base.SetupCache();

        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _mcpServerMock = new Mock<McpServer>();
        _suggestionServiceMock = new Mock<ICollectionSuggestionService>();

        // Generate a large list of collections
        _largeCollectionList = new List<Collection>();
        var random = new Random(42);
        for (int i = 0; i < CollectionCount; i++)
        {
            _largeCollectionList.Add(new Collection
            {
                Id = i,
                Title = $"Collection {i}",
                Count = random.Next(0, 1000), // Random count
                Parent = (i % 5 == 0) ? new IdRef { Id = 1 } : null // 20% have parents (should be filtered out)
            });
        }

        // Setup Bookmark GetAsync
        _raindropsApiMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, new Raindrop { Id = 123, Title = "Test Bookmark", Link = "http://example.com" }));

        // Setup Collections ListAsync
        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, _largeCollectionList));

        // Setup McpServer
        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        _suggestionServiceMock.Setup(x => x.SuggestAsync(
                It.IsAny<Raindrop>(),
                It.IsAny<IReadOnlyCollection<Collection>>(),
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CollectionSuggestion(new Collection { Id = 1, Title = "Collection 1" }, 1),
                new CollectionSuggestion(new Collection { Id = 2, Title = "Collection 2" }, 0.9),
                new CollectionSuggestion(new Collection { Id = 3, Title = "Collection 3" }, 0.8)
            ]);

        var elicitResult = new ElicitResult
        {
            Action = "decline"
        };

        _mcpServerMock.Setup(x => x.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new JsonRpcResponse { Result = JsonSerializer.SerializeToNode(elicitResult) });

        var options = Options.Create(new RaindropOptions { ApiToken = "benchmark-token" });
        _tools = new CollectionsTools(
            _collectionsApiMock.Object,
            _raindropsApiMock.Object,
            new RaindropCacheService(),
            _suggestionServiceMock.Object,
            options);
    }

    [Benchmark]
    public async Task SuggestCollectionForBookmarkAsync()
    {
        await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, 123, CancellationToken.None);
    }
}
