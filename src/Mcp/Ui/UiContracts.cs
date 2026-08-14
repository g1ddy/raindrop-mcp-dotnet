using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mcp.Ui;

public class ExplorerBookmarkSummary
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("excerpt")]
    public string? Excerpt { get; set; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }
}

public class ExplorerResult
{
    [JsonPropertyName("collectionId")]
    public int CollectionId { get; set; }

    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("perPage")]
    public int? PerPage { get; set; }

    [JsonPropertyName("nested")]
    public bool? Nested { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public IReadOnlyList<ExplorerBookmarkSummary> Items { get; set; } = new List<ExplorerBookmarkSummary>();
}

public class ExplorerBookmarkDetails
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("excerpt")]
    public string? Excerpt { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    [JsonPropertyName("created")]
    public System.DateTimeOffset? Created { get; set; }
}

public class ExplorerErrorResult
{
    [JsonPropertyName("error")]
    public required string Error { get; set; }
}
