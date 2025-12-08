using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Mcp;

namespace Mcp.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            {"Raindrop:ApiToken", "valid-token"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddRaindropApiClient(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<RaindropOptions>>().Value;

        // Assert
        Assert.Equal("valid-token", options.ApiToken);
        Assert.Equal("https://api.raindrop.io/rest/v1", options.BaseUrl);
    }

    [Fact]
    public void MissingApiToken_ShouldThrowValidationException()
    {
        // Arrange
        var configData = new Dictionary<string, string?>(); // Empty config
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddRaindropApiClient(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        // Accessing Value triggers validation
        Assert.Throws<OptionsValidationException>(() =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RaindropOptions>>().Value;
        });
    }

    [Fact]
    public void ExplicitBaseUrl_ShouldOverrideDefault()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            {"Raindrop:ApiToken", "valid-token"},
            {"Raindrop:BaseUrl", "https://custom.api.com"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddRaindropApiClient(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<RaindropOptions>>().Value;

        // Assert
        Assert.Equal("https://custom.api.com", options.BaseUrl);
    }
}
