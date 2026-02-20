using BenchmarkDotNet.Attributes;
using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Common;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class MergeCollectionsBenchmark
{
    private CollectionsTools _tools;
    private Mock<ICollectionsApi> _collectionsApiMock;
    private Mock<IRaindropsApi> _raindropsApiMock;
    private RaindropCacheService _cacheService;
    private HashSet<int> _ids;
    private int _targetId;

    [Params(10, 100, 500)]
    public int CollectionCount;

    [GlobalSetup]
    public void Setup()
    {
        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _cacheService = new RaindropCacheService();

        _targetId = 999999;
        // Generate IDs ensuring targetId is not present to avoid ArgumentException
        _ids = Enumerable.Range(1, CollectionCount).ToHashSet();

        _collectionsApiMock.Setup(x => x.MergeAsync(It.IsAny<CollectionsMergeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        _tools = new CollectionsTools(_collectionsApiMock.Object, _raindropsApiMock.Object, _cacheService);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cacheService?.Dispose();
    }

    [Benchmark]
    public async Task MergeCollectionsAsync()
    {
        await _tools.MergeCollectionsAsync(_targetId, _ids, CancellationToken.None);
    }
}
