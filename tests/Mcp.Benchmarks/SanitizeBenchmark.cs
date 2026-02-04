using BenchmarkDotNet.Attributes;
using Mcp.Collections;
using System;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class SanitizeBenchmark
{
    // A mix of normal text, pipes, and various line endings.
    [Params(
        "Simple text",
        "Text with | pipe",
        "Text with \n newline",
        "Text with \r\n windows newline",
        "Complex | text \r\n with \u0085 mixed \u2028 line \u2029 endings and | pipes | everywhere."
    )]
    public string? Input { get; set; }

    [Benchmark(Baseline = true)]
    public string Baseline()
    {
        return Input?.ReplaceLineEndings(" ").Replace("|", string.Empty) ?? string.Empty;
    }

    [Benchmark]
    public string Optimized()
    {
        return CollectionsTools.Sanitize(Input);
    }
}
