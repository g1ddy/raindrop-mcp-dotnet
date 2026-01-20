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
using Polly;
using Polly.Extensions.Http;

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
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="builderCustomizer">Optional action to customize the HTTP client builder (e.g. adding handlers).</param>
    public static IServiceCollection AddRaindropApiClient(this IServiceCollection services, IConfiguration configuration, Action<IHttpClientBuilder>? builderCustomizer = null)
    {
        services.AddOptions<RaindropOptions>()
            .Bind(configuration.GetSection("Raindrop"))
            .ValidateDataAnnotations()
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

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx and 408
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        void AddClient<T>(RefitSettings refitSettings) where T : class
        {
            var builder = services.AddRefitClient<T>(refitSettings).ConfigureHttpClient(Configure);

            // Add Polly policy
            builder.AddPolicyHandler(retryPolicy);

            builderCustomizer?.Invoke(builder);
        }

        AddClient<ICollectionsApi>(settings);
        AddClient<IRaindropsApi>(settings);
        AddClient<IHighlightsApi>(settings);
        AddClient<IFiltersApi>(settings);
        AddClient<ITagsApi>(settings);
        AddClient<IUserApi>(settings);

        return services;
    }
}
