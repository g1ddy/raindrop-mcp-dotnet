using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Filters;

/// <summary>
/// Represents filter statistics for a collection.
/// </summary>
[Description("Filter metadata returned by Raindrop.io")]
public record AvailableFilters
{
    [Description("Indicates if the request succeeded")]
    public bool Result { get; init; }
    [Description("Broken links count")]
    public FilterMetric? Broken { get; init; }

    [Description("Duplicate links count")]
    public FilterMetric? Duplicates { get; init; }

    [Description("Count of raindrops marked as favorite")]
    public FilterMetric? Important { get; init; }

    [JsonPropertyName("notag")]
    [Description("Count of raindrops without any tag")]
    public FilterMetric? NoTag { get; init; }

    [Description("List of tags with counts")]
    public List<FilterEntry>? Tags { get; init; }

    [Description("List of types with counts")]
    public List<FilterEntry>? Types { get; init; }
}

[Description("Count of items for a filter")]
public record FilterMetric([property: Description("Number of matching items")] int Count);

[Description("Filter entry with identifier and count")]
public record FilterEntry
{
    [JsonPropertyName("_id")]
    [Description("Tag or type identifier")]
    public string Id { get; init; } = string.Empty;

    [Description("Number of matching items")]
    public int Count { get; init; }
}
