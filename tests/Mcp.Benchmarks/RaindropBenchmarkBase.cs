using BenchmarkDotNet.Attributes;
using Mcp.Common;

namespace Mcp.Benchmarks;

public abstract class RaindropBenchmarkBase
{
    protected RaindropCacheService CacheService { get; private set; }

    [GlobalSetup]
    public virtual void SetupCache()
    {
        CacheService = new RaindropCacheService();
    }

    [GlobalCleanup]
    public virtual void CleanupCache()
    {
        CacheService?.Dispose();
    }
}
