using System.ComponentModel.DataAnnotations;

namespace Mcp;

/// <summary>
/// Options for configuring the Raindrop API client.
/// </summary>
public class RaindropOptions
{
    /// <summary>
    /// API token used for authenticating with the Raindrop API.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the Raindrop API.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; set; } = "https://api.raindrop.io/rest/v1";
}
