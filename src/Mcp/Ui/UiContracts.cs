using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mcp.Ui;

public class ExplorerBookmarkSummary
{

    public long Id { get; set; }


    public string? Title { get; set; }


    public string? Link { get; set; }


    public string? Domain { get; set; }


    public string? Excerpt { get; set; }


    public IReadOnlyList<string>? Tags { get; set; }
}

public class ExplorerResult
{

    public int CollectionId { get; set; }


    public string? Search { get; set; }


    public string? Sort { get; set; }


    public int? Page { get; set; }


    public int? PerPage { get; set; }


    public bool? Nested { get; set; }


    public int Count { get; set; }


    public IReadOnlyList<ExplorerBookmarkSummary> Items { get; set; } = new List<ExplorerBookmarkSummary>();
}

public class ExplorerBookmarkDetails
{

    public long Id { get; set; }


    public string? Title { get; set; }


    public string? Link { get; set; }


    public string? Domain { get; set; }


    public string? Excerpt { get; set; }


    public string? Note { get; set; }


    public IReadOnlyList<string>? Tags { get; set; }


    public System.DateTimeOffset? Created { get; set; }
}

public class ExplorerErrorResult
{

    public required string Error { get; set; }
}
