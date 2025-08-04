namespace Mcp;

/// <summary>
/// Options for configuring the Raindrop API client.
/// </summary>
public class RaindropOptions
{
    /// <summary>
    /// API token used for authenticating with the Raindrop API.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Base URL of the Raindrop API.
    /// </summary>
    public string? BaseUrl { get; set; }
}
