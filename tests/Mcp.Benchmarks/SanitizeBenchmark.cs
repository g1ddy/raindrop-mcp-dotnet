using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

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
    public string OptimizedStringCreate()
    {
        return SanitizeStringCreate(Input);
    }

    [Benchmark]
    public string OptimizedStackAlloc()
    {
        return SanitizeStackAlloc(Input);
    }

    public static string SanitizeStringCreate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        int length = value.Length;
        bool needsSanitization = false;

        for (int i = 0; i < length; i++)
        {
            char c = value[i];
            if (c == '|' || c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029')
            {
                needsSanitization = true;
                break;
            }
        }

        if (!needsSanitization) return value;

        int newLength = 0;
        for (int i = 0; i < length; i++)
        {
            char c = value[i];
            if (c == '|') continue;
            if (c == '\r')
            {
                newLength++;
                if (i + 1 < length && value[i + 1] == '\n') i++;
            }
            else if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
            {
                newLength++;
            }
            else
            {
                newLength++;
            }
        }

        return string.Create(newLength, value, (span, state) =>
        {
            var input = state.AsSpan();
            int inputLen = input.Length;
            int idx = 0;
            for (int i = 0; i < inputLen; i++)
            {
                char c = input[i];
                if (c == '|') continue;
                if (c == '\r')
                {
                    span[idx++] = ' ';
                    if (i + 1 < inputLen && input[i + 1] == '\n') i++;
                }
                else if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                {
                    span[idx++] = ' ';
                }
                else
                {
                    span[idx++] = c;
                }
            }
        });
    }

    public static string SanitizeStackAlloc(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        int length = value.Length;
        bool needsSanitization = false;

        for (int i = 0; i < length; i++)
        {
            char c = value[i];
            if (c == '|' || c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029')
            {
                needsSanitization = true;
                break;
            }
        }

        if (!needsSanitization) return value;

        if (length <= 512)
        {
            Span<char> buffer = stackalloc char[length];
            int idx = 0;
            for (int i = 0; i < length; i++)
            {
                char c = value[i];
                if (c == '|') continue;
                if (c == '\r')
                {
                    buffer[idx++] = ' ';
                    if (i + 1 < length && value[i + 1] == '\n') i++;
                }
                else if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                {
                    buffer[idx++] = ' ';
                }
                else
                {
                    buffer[idx++] = c;
                }
            }
            return new string(buffer.Slice(0, idx));
        }
        else
        {
             // Fallback to string.Create for large strings
            return SanitizeStringCreate(value);
        }
    }
}
