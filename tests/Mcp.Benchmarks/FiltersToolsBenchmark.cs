using BenchmarkDotNet.Attributes;
using Mcp.Filters;
using Mcp.Common;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class FiltersToolsBenchmark
{
    private FiltersTools _tools = null!;
    private Mock<IFiltersApi> _filtersApiMock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _filtersApiMock = new Mock<IFiltersApi>();

        // Mock GetAsync with a slight delay to simulate network latency
        _filtersApiMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(async (long id, string? sort, string? search, CancellationToken ct) => {
                await Task.Delay(50, ct);
                return new AvailableFilters { Result = true };
            });

        var options = Options.Create(new RaindropOptions { ApiToken = "benchmark-token" });
        _tools = new FiltersTools(_filtersApiMock.Object, new RaindropCacheService(), options);
    }

    [Benchmark]
    public async Task GetAvailableFilters()
    {
        await _tools.GetAvailableFiltersAsync(0, null, null, CancellationToken.None);
    }
}
