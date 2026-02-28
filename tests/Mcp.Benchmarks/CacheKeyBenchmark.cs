using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class CacheKeyBenchmark
{
    private string _token = "12345678-1234-1234-1234-123456789012-12345678-1234-1234-1234-123456789012";

    [Benchmark(Baseline = true)]
    public string Original()
    {
        var bytes = Encoding.UTF8.GetBytes(_token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    [Benchmark]
    public string Optimized()
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(_token.Length);
        byte[]? rentedBytes = null;
        Span<byte> buffer = maxByteCount <= 512
            ? stackalloc byte[maxByteCount]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(maxByteCount));

        try
        {
            int byteCount = Encoding.UTF8.GetBytes(_token, buffer);
            Span<byte> hashBuffer = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(buffer.Slice(0, byteCount), hashBuffer);
            return Convert.ToHexString(hashBuffer);
        }
        finally
        {
            if (rentedBytes != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBytes);
            }
        }
    }
}
