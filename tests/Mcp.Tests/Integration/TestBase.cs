using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

public abstract class TestBase
{
    private readonly IServiceProvider? _provider;

    protected IServiceProvider Provider
    {
        get
        {
            RequireApi();
            return _provider!;
        }
    }

    private readonly bool _isConfigured;

    protected TestBase(params Action<IServiceCollection>[] registrations)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var token = config["Raindrop:ApiToken"];
        // Support environment variable format Raindrop__ApiToken which maps to Raindrop:ApiToken

        if (!string.IsNullOrWhiteSpace(token))
        {
            _isConfigured = true;
            var services = new ServiceCollection();
            services.AddRaindropApiClient(config);
            foreach (var reg in registrations) reg(services);
            _provider = services.BuildServiceProvider();
        }
        else
        {
             _provider = null;
        }
    }

    protected void RequireApi()
    {
        Skip.IfNot(_isConfigured, "Raindrop API Token not configured (Raindrop:ApiToken or Raindrop__ApiToken)");
    }
}
