## 2024-05-24 - Zero-allocation Cache Key Hashing
**Learning:** Computing cache keys by hashing strings (e.g., API tokens) using `Encoding.UTF8.GetBytes()` followed by `SHA256.HashData(byte[])` causes unnecessary heap allocations for the byte array on every cache access.
**Action:** Use `stackalloc byte[]` for small strings combined with `ArrayPool<byte>.Shared.Rent` for larger ones, and pass `Span<byte>` to `SHA256.HashData`. This reduces allocations per hash by ~50% (from 312B to 152B for a typical token), allocating only the final hex string.

## 2024-05-24 - Zero-allocation List Chunking
**Learning:** Using `IEnumerable.Chunk()` allocates new arrays (`T[]`) for every chunk generated, which creates significant GC pressure in bulk operations where `IReadOnlyList<T>` is required by the downstream serializer (System.Text.Json).
**Action:** When a bulk payload requires `IReadOnlyList<T>`, use a custom struct or class (e.g., `ReadOnlyListSlice<T>`) that implements both `IReadOnlyList<T>` and `IList<T>` (for STJ optimization) and wraps the original list with an offset and count. This achieves zero-allocation chunking and reduces memory pressure drastically.
