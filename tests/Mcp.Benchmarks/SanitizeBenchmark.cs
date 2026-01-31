using System.Text;
using BenchmarkDotNet.Attributes;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class SanitizeBenchmark
{
    private const string CleanString = "My Collection";
    private const string DirtyString = "My\nCollection|Target";
    private const string VeryDirtyString = "My\r\nCollection|Target|One\nTwo|Three";

    [Benchmark(Baseline = true)]
    [Arguments(CleanString)]
    [Arguments(DirtyString)]
    [Arguments(VeryDirtyString)]
    public string Original(string value)
    {
        return value?.ReplaceLineEndings(" ").Replace("|", string.Empty) ?? string.Empty;
    }

    [Benchmark]
    [Arguments(CleanString)]
    [Arguments(DirtyString)]
    [Arguments(VeryDirtyString)]
    public string OptimizedStringBuilder(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        int idx = -1;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '|' || c == '\r' || c == '\n')
            {
                idx = i;
                break;
            }
        }

        if (idx == -1) return value;

        var sb = new StringBuilder(value.Length);
        sb.Append(value, 0, idx);

        for (int i = idx; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '|') continue;

            if (c == '\r')
            {
                sb.Append(' ');
                if (i + 1 < value.Length && value[i + 1] == '\n')
                {
                    i++;
                }
            }
            else if (c == '\n')
            {
                sb.Append(' ');
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    [Benchmark]
    [Arguments(CleanString)]
    [Arguments(DirtyString)]
    [Arguments(VeryDirtyString)]
    public string OptimizedStackAlloc(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        int idx = -1;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '|' || c == '\r' || c == '\n')
            {
                idx = i;
                break;
            }
        }

        if (idx == -1) return value;

        int len = value.Length;
        if (len <= 512)
        {
            Span<char> buffer = stackalloc char[len];
            value.AsSpan(0, idx).CopyTo(buffer);
            int pos = idx;

            for (int i = idx; i < len; i++)
            {
                char c = value[i];
                if (c == '|') continue;

                if (c == '\r')
                {
                    buffer[pos++] = ' ';
                    if (i + 1 < len && value[i + 1] == '\n') i++;
                }
                else if (c == '\n')
                {
                    buffer[pos++] = ' ';
                }
                else
                {
                    buffer[pos++] = c;
                }
            }

            return new string(buffer.Slice(0, pos));
        }
        else
        {
             var sb = new StringBuilder(len);
             sb.Append(value, 0, idx);

             for (int i = idx; i < value.Length; i++)
             {
                 char c = value[i];
                 if (c == '|') continue;

                 if (c == '\r')
                 {
                     sb.Append(' ');
                     if (i + 1 < value.Length && value[i + 1] == '\n')
                     {
                         i++;
                     }
                 }
                 else if (c == '\n')
                 {
                     sb.Append(' ');
                 }
                 else
                 {
                     sb.Append(c);
                 }
             }

             return sb.ToString();
        }
    }
}
