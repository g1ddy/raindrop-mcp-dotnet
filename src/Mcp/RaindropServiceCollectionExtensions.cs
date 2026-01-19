using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using System.Net.Http.Headers;
using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Highlights;
using Mcp.Filters;
using Mcp.Tags;
using Mcp.User;

namespace Mcp;

/// <summary>
/// Extension methods for registering Raindrop API services.
/// </summary>
public static class RaindropServiceCollectionExtensions
{
    /// <summary>
    /// Registers Raindrop API clients using configuration from the
    /// "Raindrop" section of <see cref="IConfiguration"/>.
    /// </summary>
    public static IServiceCollection AddRaindropApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RaindropOptions>()
            .Bind(configuration.GetSection("Raindrop"))
            .ValidateDataAnnotations()
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Raindrop:BaseUrl must be an absolute URI.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiToken), "Raindrop:ApiToken must be configured.")
            .ValidateOnStart();

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            })
        };

        void Configure(IServiceProvider sp, HttpClient client)
        {
            var options = sp.GetRequiredService<IOptions<RaindropOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
        }

        services.AddRefitClient<ICollectionsApi>(settings).ConfigureHttpClient(Configure);
        services.AddRefitClient<IRaindropsApi>(settings).ConfigureHttpClient(Configure);
        services.AddRefitClient<IHighlightsApi>(settings).ConfigureHttpClient(Configure);
        services.AddRefitClient<IFiltersApi>(settings).ConfigureHttpClient(Configure);
        services.AddRefitClient<ITagsApi>(settings).ConfigureHttpClient(Configure);
        services.AddRefitClient<IUserApi>(settings).ConfigureHttpClient(Configure);

        return services;
    }
}
