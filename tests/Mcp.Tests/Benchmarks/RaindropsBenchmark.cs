using Mcp.Raindrops;
using Mcp.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Mcp.Tests.Benchmarks;

[Trait("Category", "Benchmark")]
public class RaindropsBenchmark : TestBase
{
    private readonly ITestOutputHelper _output;

    public RaindropsBenchmark(ITestOutputHelper output) : base(s => s.AddTransient<RaindropsTools>())
    {
        _output = output;
    }

    [Fact]
    public async Task BenchmarkAllocation()
    {
        InitializeVcr();

        var tool = Provider.GetRequiredService<RaindropsTools>();
        var uniqueId = CurrentTestId;
        _output.WriteLine($"TestID: {uniqueId}");

        // Create 1000 items matching the fixture
        var items = new List<Raindrop>();
        for (int i = 0; i < 1000; i++)
        {
            items.Add(new Raindrop
            {
                Title = $"Bench Item {i} {uniqueId}",
                Link = $"https://example.com/bench{i}/{uniqueId}"
            });
        }

        // Warmup
        await tool.CreateBookmarksAsync(0, items, CancellationToken.None);

        // Force GC to clear up warmup garbage
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Measure
        long totalBytes = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int iterations = 100;

        for (int i = 0; i < iterations; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            await tool.CreateBookmarksAsync(0, items, CancellationToken.None);
            long after = GC.GetAllocatedBytesForCurrentThread();
            totalBytes += (after - before);
        }

        stopwatch.Stop();

        _output.WriteLine($"Total Allocations ({iterations} runs): {totalBytes / 1024.0:F2} KB");
        _output.WriteLine($"Average Allocation per run: {totalBytes / iterations} bytes");
        _output.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds} ms");
    }
}
