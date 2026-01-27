using BenchmarkDotNet.Running;

namespace Mcp.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<CollectionsToolsBenchmark>();
    }
}
