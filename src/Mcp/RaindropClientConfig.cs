using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Mcp;

/// <summary>
/// Cached configuration for Raindrop API clients to avoid repeated parsing of options.
/// </summary>
internal sealed class RaindropClientConfig
{
    public Uri BaseUri { get; }
    public AuthenticationHeaderValue AuthorizationHeader { get; }

    public RaindropClientConfig(IOptions<RaindropOptions> options)
    {
        var opts = options.Value;
        BaseUri = new Uri(opts.BaseUrl);
        AuthorizationHeader = new AuthenticationHeaderValue("Bearer", opts.ApiToken);
    }
}
