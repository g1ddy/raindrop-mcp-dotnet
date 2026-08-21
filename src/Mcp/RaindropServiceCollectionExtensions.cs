using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using System.Net.Http.Headers;
using Mcp.Common;
using Mcp.Collections;
using Mcp.Collections.Suggestions;
using Mcp.Raindrops;
using Mcp.Highlights;
using Mcp.Filters;
using Mcp.Tags;
using Mcp.User;
using Mcp.Analytics;
using Polly;
using Polly.Extensions.Http;
using LocalEmbeddings;
using LocalEmbeddings.Options;
using Microsoft.Extensions.AI;


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

        services.AddOptions<LibraryAnalyticsOptions>()
            .Bind(configuration.GetSection("Analytics"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<RaindropClientConfig>();
        services.AddSingleton<IRaindropCacheService, RaindropCacheService>();
        services.AddSingleton<CollectionSuggestionIndexCache>();
        services.AddTransient<ICollectionSuggestionService, CollectionSuggestionService>();
        services.AddTransient<ILibraryAnalyticsService, LibraryAnalyticsService>();

        if (configuration.GetSection("Raindrop").GetValue<bool>("EnableSemanticSuggestions", true))
        {
            // Note: because DI resolving fails if an interface isn't registered but is requested without nullability
            // in some frameworks, we could use try-catch and NOT register it. However, the Microsoft DI container doesn't
            // allow a factory to return null for AddSingleton<T> (where T is a reference type).
            // The cleanest way is to just conditionally not register it! But what if a test needs it or it's requested?
            // CollectionSuggestionService takes it as `IEmbeddingGenerator?`.
            // So if we don't register it, DI injects null. Perfect!

            // BUT! The LocalEmbeddingGenerator requires downloading models dynamically which might fail during the factory lambda
            // if we put a try/catch inside it. Let's register it via a factory that returns null if instantiation fails?
            // Microsoft DI throws if a factory returns null for a required service.
            // Let's stick with the try-catch returning Dummy if it fails, and just ensure Dummy is safe.

            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RaindropOptions>>().Value;
                if (!options.EnableSemanticSuggestions) return new DummyEmbeddingGenerator();

                try
                {
                    return new LocalEmbeddingGenerator(new LocalEmbeddingsOptions());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to load LocalEmbeddingGenerator, falling back to dummy: {ex.Message}");
                    return new DummyEmbeddingGenerator();
                }
            });
        }



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
            var config = sp.GetRequiredService<RaindropClientConfig>();

            client.BaseAddress = config.BaseUri;
            client.DefaultRequestHeaders.Authorization = config.AuthorizationHeader;
        }

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx and 408
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                3,
                (retryAttempt, response, _) =>
                {
                    var retryAfter = response.Result?.Headers.RetryAfter;
                    if (retryAfter?.Delta is { } delta)
                        return delta;

                    if (retryAfter?.Date is { } date)
                        return date > DateTimeOffset.UtcNow ? date - DateTimeOffset.UtcNow : TimeSpan.Zero;

                    // Exponential backoff with jitter avoids synchronized retries across clients.
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                },
                (_, _, _, _) => Task.CompletedTask);

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
