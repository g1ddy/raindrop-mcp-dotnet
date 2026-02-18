using BenchmarkDotNet.Attributes;
using Mcp.Collections;
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
public class CollectionsToolsBenchmark
{
    private CollectionsTools _tools;
    private Mock<McpServer> _mcpServerMock;
    private Mock<ICollectionsApi> _collectionsApiMock;
    private Mock<IRaindropsApi> _raindropsApiMock;
    private List<Collection> _largeCollectionList;

    [Params(100, 1000)]
    public int CollectionCount;

    [GlobalSetup]
    public void Setup()
    {
        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _mcpServerMock = new Mock<McpServer>();

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
                Sampling = new SamplingCapability(),
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        // Mock SendRequestAsync to return a valid response
        var llmResponse = new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "Collection 1 | Collection 2 | Collection 3" }],
            Model = "test-model"
        };

        _mcpServerMock.Setup(x => x.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new JsonRpcResponse { Result = JsonSerializer.SerializeToNode(llmResponse) });

        var options = Options.Create(new RaindropOptions { ApiToken = "benchmark-token" });
        _tools = new CollectionsTools(_collectionsApiMock.Object, _raindropsApiMock.Object, new RaindropCacheService(), options);
    }

    [Benchmark]
    public async Task SuggestCollectionForBookmarkAsync()
    {
        await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, 123, CancellationToken.None);
    }
}
