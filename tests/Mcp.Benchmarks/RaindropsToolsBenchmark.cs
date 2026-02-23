using BenchmarkDotNet.Attributes;
using Mcp.Raindrops;
using Mcp.Common;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class RaindropsToolsBenchmark : RaindropBenchmarkBase
{
    private RaindropsTools _tools;
    private Mock<IRaindropsApi> _raindropsApiMock;
    private List<Raindrop> _preAllocatedList;
    private const int ItemCount = 100;

    public override void SetupCache()
    {
        base.SetupCache();
        _raindropsApiMock = new Mock<IRaindropsApi>();

        // Setup CreateManyAsync to return immediately
        _raindropsApiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, new List<Raindrop>()));

        var options = Options.Create(new RaindropOptions { ApiToken = "bench-token" });
        _tools = new RaindropsTools(_raindropsApiMock.Object, new RaindropCacheService(), options);

        _preAllocatedList = new List<Raindrop>(ItemCount);
        for (int i = 0; i < ItemCount; i++)
        {
            _preAllocatedList.Add(new Raindrop { Title = $"Item {i}", Link = $"https://example.com/{i}" });
        }
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
