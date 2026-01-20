namespace Mcp.Tests.Integration.Infrastructure;

public class TestScenario
{
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<RecordedInteraction> Interactions { get; set; } = new();
}

public class RecordedInteraction
{
    public string Method { get; set; } = "";
    public string Uri { get; set; } = "";
    public string? RequestBody { get; set; }

    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public Dictionary<string, string[]> ResponseHeaders { get; set; } = new();
    public Dictionary<string, string[]> ContentHeaders { get; set; } = new();
}
