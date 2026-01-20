using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Runtime.CompilerServices;
using Mcp.Tests.Integration.Infrastructure;

namespace Mcp.Tests.Integration;

public abstract class TestBase : IDisposable
{
    private IServiceProvider? _provider;
    private readonly IConfiguration _config;
    private readonly Action<IServiceCollection>[] _registrations;
    private RecordingHandler? _recordingHandler;
    private bool _isReplaying;

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
        _registrations = registrations;
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var token = _config["Raindrop:ApiToken"];
        // Support environment variable format Raindrop__ApiToken which maps to Raindrop:ApiToken

        if (!string.IsNullOrWhiteSpace(token))
        {
            _isConfigured = true;
            // Build default provider without recording
            _provider = BuildServiceProvider(null);
        }
        else
        {
             _provider = null;
        }
    }

    private IServiceProvider BuildServiceProvider(RecordingHandler? recordingHandler)
    {
        var services = new ServiceCollection();

        Action<IHttpClientBuilder>? customizer = null;
        if (recordingHandler != null)
        {
            services.AddSingleton(recordingHandler);
            customizer = builder => builder.AddHttpMessageHandler(_ => recordingHandler);
        }

        IConfiguration configToUse = _config;
        if (_isReplaying)
        {
            // Inject dummy token to satisfy validation
            configToUse = new ConfigurationBuilder()
                .AddConfiguration(_config)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Raindrop:ApiToken", "dummy-token-for-replay" }
                })
                .Build();
        }

        services.AddRaindropApiClient(configToUse, customizer);
        foreach (var reg in _registrations) reg(services);
        return services.BuildServiceProvider();
    }

    protected void RequireApi()
    {
        Skip.IfNot(_isConfigured || _isReplaying, "Raindrop API Token not configured and no replay data available.");
    }

    protected void InitializeVcr([CallerMemberName] string testName = "", [CallerFilePath] string sourceFilePath = "")
    {
        var integrationDir = Path.GetDirectoryName(sourceFilePath);
        var baseDir = integrationDir != null && Directory.Exists(integrationDir)
            ? integrationDir // Source checkout / local dev
            : Directory.GetCurrentDirectory(); // Fallback to bin

        var fixturePath = Path.Combine(baseDir, "Fixtures", GetType().Name, $"{testName}.json");

        if (_isConfigured)
        {
            // Record Mode
            _recordingHandler = new RecordingHandler(fixturePath, RecordingMode.Record);
            // Rebuild provider with recording
            (_provider as IDisposable)?.Dispose();
            _provider = BuildServiceProvider(_recordingHandler);
        }
        else if (File.Exists(fixturePath))
        {
            // Replay Mode
            _isReplaying = true;
            _recordingHandler = new RecordingHandler(fixturePath, RecordingMode.Replay);
            _provider = BuildServiceProvider(_recordingHandler);
        }
        else
        {
            // No token, no file -> Do nothing, RequireApi will skip
        }
    }

    protected string CurrentTestId
    {
        get
        {
            // If VCR is not initialized, fallback to random (e.g. strict Record mode or no-VCR tests)
            if (_recordingHandler == null) return Guid.NewGuid().ToString("N");

            const string key = "TestId";
            var existing = _recordingHandler.GetMetadata(key);
            if (existing != null) return existing;

            // Generate new
            var newId = Guid.NewGuid().ToString("N");
            _recordingHandler.SetMetadata(key, newId);
            return newId;
        }
    }

    public void Dispose()
    {
        _recordingHandler?.Save();
        (_provider as IDisposable)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
