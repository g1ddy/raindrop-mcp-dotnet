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

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class CollectionsToolsBenchmark
{
    private CollectionsTools _tools;
    private Mock<McpServer> _mcpServerMock;
    private Mock<ICollectionsApi> _collectionsApiMock;
    private Mock<IRaindropsApi> _raindropsApiMock;

    [GlobalSetup]
    public void Setup()
    {
        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _mcpServerMock = new Mock<McpServer>();

        // Setup Bookmark GetAsync with 100ms delay
        _raindropsApiMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(async (long id, CancellationToken ct) => {
                await Task.Delay(100, ct);
                return new ItemResponse<Raindrop>(true, new Raindrop { Id = id, Title = "Test Bookmark", Link = "http://example.com" });
            });

        // Setup Collections ListAsync with 100ms delay
        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => {
                await Task.Delay(100, ct);
                return new ItemsResponse<Collection>(true, new List<Collection> { new Collection { Id = 1, Title = "Test Collection" } });
            });

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
            Content = [new TextContentBlock { Text = "Test Collection" }],
            Model = "test-model"
        };

        _mcpServerMock.Setup(x => x.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new JsonRpcResponse { Result = JsonSerializer.SerializeToNode(llmResponse) });

        _tools = new CollectionsTools(_collectionsApiMock.Object, _raindropsApiMock.Object);
    }

    [Benchmark]
    public async Task SuggestCollectionForBookmarkAsync()
    {
        await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, 123, CancellationToken.None);
    }
}
