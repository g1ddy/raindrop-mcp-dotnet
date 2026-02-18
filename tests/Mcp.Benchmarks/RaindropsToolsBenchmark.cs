using BenchmarkDotNet.Attributes;
using Mcp.Raindrops;
using Mcp.Common;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class RaindropsToolsBenchmark
{
    private RaindropsTools _tools;
    private Mock<IRaindropsApi> _raindropsApiMock;
    private RaindropCacheService _cacheService;
    private List<Raindrop> _preAllocatedList;
    private const int ItemCount = 100;

    [GlobalSetup]
    public void Setup()
    {
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _cacheService = new RaindropCacheService();

        // Setup CreateManyAsync to return immediately
        _raindropsApiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, new List<Raindrop>()));

        _tools = new RaindropsTools(_raindropsApiMock.Object, _cacheService);

        _preAllocatedList = new List<Raindrop>(ItemCount);
        for (int i = 0; i < ItemCount; i++)
        {
            _preAllocatedList.Add(new Raindrop { Title = $"Item {i}", Link = $"https://example.com/{i}" });
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cacheService.Dispose();
    }

    [Benchmark]
    public async Task CreateBookmarksAllocatedList()
    {
        await _tools.CreateBookmarksAsync(0, _preAllocatedList, CancellationToken.None);
    }

    [Benchmark]
    public async Task CreateBookmarksEnumerable()
    {
        await _tools.CreateBookmarksAsync(0, GenerateItems(), CancellationToken.None);
    }

    private IEnumerable<Raindrop> GenerateItems()
    {
        foreach (var item in _preAllocatedList)
        {
            yield return item;
        }
    }
}
