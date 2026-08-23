using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Mcp.Analytics;
using RaindropMcp.Benchmarks.Fakes;

namespace RaindropMcp.Benchmarks;

[MemoryDiagnoser]
public class LibraryAnalyticsBenchmark
{
    public enum WorkloadSize
    {
        Small,
        Medium,
        Large
    }

    [Params(WorkloadSize.Small, WorkloadSize.Medium, WorkloadSize.Large)]
    public WorkloadSize Size { get; set; }

    private LibraryAnalyticsService _service = null!;
    private FakeRaindropsApi _raindropsApi = null!;
    private FakeCollectionsApi _collectionsApi = null!;

    [GlobalSetup]
    public void Setup()
    {
        var (bookmarks, collections) = Size switch
        {
            WorkloadSize.Small => (50, 10),
            WorkloadSize.Medium => (1000, 100),
            WorkloadSize.Large => (10000, 1000),
            _ => throw new ArgumentOutOfRangeException()
        };

        _raindropsApi = new FakeRaindropsApi(bookmarks, collections);
        _collectionsApi = new FakeCollectionsApi(collections);

        _service = new LibraryAnalyticsService(
            _collectionsApi,
            _raindropsApi,
            Microsoft.Extensions.Options.Options.Create(new LibraryAnalyticsOptions { MaximumPages = 10000 }));
    }

    [Benchmark]
    public async Task<LibraryAnalyticsReport> AnalyzeLibraryAsync()
    {
        return await _service.AnalyzeAsync(0, CancellationToken.None);
    }
}
