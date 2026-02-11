using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mcp;
using Mcp.User;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class HttpClientConfigurationBenchmark
{
    private IServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Raindrop:ApiToken", "test-token"},
                {"Raindrop:BaseUrl", "https://api.raindrop.io/rest/v1"}
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRaindropApiClient(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Benchmark]
    public void ResolveClient()
    {
        // Resolving IUserApi creates a transient client, which uses IHttpClientFactory to configure a new HttpClient.
        // This triggers the Configure delegate we want to optimize.
        _ = _serviceProvider.GetRequiredService<IUserApi>();
    }
}
