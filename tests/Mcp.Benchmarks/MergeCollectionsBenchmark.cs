using BenchmarkDotNet.Attributes;
using Mcp.Collections;
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
public class MergeCollectionsBenchmark : RaindropBenchmarkBase
{
    private CollectionsTools _tools;
    private Mock<ICollectionsApi> _collectionsApiMock;
    private Mock<IRaindropsApi> _raindropsApiMock;
    private HashSet<int> _ids;
    private int _targetId;

    [Params(10, 100, 500)]
    public int CollectionCount;

    public override void SetupCache()
    {
        base.SetupCache();
        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();

        _targetId = 999999;
        // Generate IDs ensuring targetId is not present to avoid ArgumentException
        _ids = Enumerable.Range(1, CollectionCount).ToHashSet();

        _collectionsApiMock.Setup(x => x.MergeAsync(It.IsAny<CollectionsMergeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        var options = Options.Create(new RaindropOptions { ApiToken = "benchmark-token" });
        _tools = new CollectionsTools(_collectionsApiMock.Object, _raindropsApiMock.Object, new RaindropCacheService(), options);
    }

    [Benchmark]
    public async Task MergeCollectionsAsync()
    {
        await _tools.MergeCollectionsAsync(_targetId, _ids, CancellationToken.None);
    }
}
