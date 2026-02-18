using BenchmarkDotNet.Attributes;
using Mcp.Tags;
using Mcp.Common;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class TagsToolsBenchmark
{
    private TagsTools _tools;
    private Mock<ITagsApi> _tagsApiMock;
    private Mock<McpServer> _mcpServerMock;
    private RaindropCacheService _cacheService;
    private List<string> _tags10;
    private List<string> _tags50;
    private List<string> _tags100;

    [GlobalSetup]
    public void Setup()
    {
        _tagsApiMock = new Mock<ITagsApi>();
        _cacheService = new RaindropCacheService();
        _tools = new TagsTools(_tagsApiMock.Object, _cacheService);

        _mcpServerMock = new Mock<McpServer>();
        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities { Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() } });

        // Mock SendRequestAsync to return immediate success for elicitation
        _mcpServerMock
            .Setup(x => x.SendRequestAsync(
                It.Is<JsonRpcRequest>(r => r.Method == "elicitation/create"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse
            {
                Result = JsonSerializer.SerializeToNode(new ElicitResult
                {
                    Action = "accept",
                    Content = new Dictionary<string, JsonElement>
                    {
                        ["confirm"] = JsonSerializer.SerializeToElement(true)
                    }
                })
            });

        // Mock DeleteAsync to do nothing
        _tagsApiMock.Setup(x => x.DeleteAsync(It.IsAny<TagDeleteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        _tags10 = GenerateTags(10);
        _tags50 = GenerateTags(50);
        _tags100 = GenerateTags(100);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cacheService.Dispose();
    }

    private List<string> GenerateTags(int count)
    {
        var list = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            // Include some chars that need escaping/replacing to exercise that logic
            list.Add($"Tag \"{i}\"\nNewline");
        }
        return list;
    }

    [Benchmark]
    public async Task DeleteTags_10()
    {
        await _tools.DeleteTagsAsync(_mcpServerMock.Object, _tags10, null, CancellationToken.None);
    }

    [Benchmark]
    public async Task DeleteTags_50()
    {
        await _tools.DeleteTagsAsync(_mcpServerMock.Object, _tags50, null, CancellationToken.None);
    }

    [Benchmark]
    public async Task DeleteTags_100()
    {
        await _tools.DeleteTagsAsync(_mcpServerMock.Object, _tags100, null, CancellationToken.None);
    }
}
