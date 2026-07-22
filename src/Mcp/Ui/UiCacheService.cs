using System;
using System.Collections.Concurrent;

namespace Mcp.Ui;

public class UiCacheService : IUiCacheService
{
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public string StoreHtml(string html)
    {
        var key = Guid.NewGuid().ToString("N");
        _cache[key] = html;
        return key;
    }

    public string? GetHtml(string key)
    {
        _cache.TryGetValue(key, out var html);
        return html;
    }
}
