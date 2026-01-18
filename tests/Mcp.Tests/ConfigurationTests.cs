using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace RaindropMcp.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void DevelopmentSettings_ShouldBeLoaded_InDevelopmentEnvironment()
        {
            // Arrange
            // Force the environment to Development
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

            try
            {
                var settings = new HostApplicationBuilderSettings
                {
                    EnvironmentName = "Development"
                };

                var builder = Host.CreateApplicationBuilder(settings);

                // Act
                var logLevel = builder.Configuration["Logging:LogLevel:Default"];

                // Assert
                // It should be "Information" (from appsettings.Development.json)
                // If the file is missing, it will fall back to "Warning" (from appsettings.json)
                Assert.Equal("Information", logLevel);
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            }
        }
    }
}
