using Mcp.Tests.Integration.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Mcp.Tests.Integration;

public abstract class TestBase : IDisposable
{
    private IServiceProvider? _provider;
    private readonly IConfiguration _config;
    private readonly Action<IServiceCollection>[] _registrations;
    private RecordingState? _recordingState;
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

    private IServiceProvider BuildServiceProvider(RecordingState? recordingState)
    {
        var services = new ServiceCollection();

        Action<IHttpClientBuilder>? customizer = null;
        if (recordingState != null)
        {
            services.AddSingleton(recordingState);
            customizer = builder => builder.AddHttpMessageHandler(_ => new RecordingHandler(recordingState));
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

    protected (CancellationTokenSource Cts, CancellationToken CancellationToken, string UniqueId) SetupTestForVcr(TimeSpan? timeout = null, [CallerMemberName] string testName = "", [CallerFilePath] string sourceFilePath = "")
    {
        InitializeVcr(testName, sourceFilePath);
        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        var uniqueId = CurrentTestId;
        return (cts, cts.Token, uniqueId);
    }

    protected void InitializeVcr([CallerMemberName] string testName = "", [CallerFilePath] string sourceFilePath = "")
    {
        var fixturePath = GetFixturePath(sourceFilePath, testName);
        var forceRecord = _config.GetValue<bool>("VCR_FORCE_RECORD");

        if (_isConfigured && (forceRecord || !File.Exists(fixturePath)))
        {
            Console.Error.WriteLine($"[VCR] Mode: RECORD. Saving to: {fixturePath}");
            // Record Mode
            _recordingState = new RecordingState(fixturePath, RecordingMode.Record);
            // Rebuild provider with recording
            (_provider as IDisposable)?.Dispose();
            _provider = BuildServiceProvider(_recordingState);
        }
        else if (File.Exists(fixturePath))
        {
            Console.Error.WriteLine($"[VCR] Mode: REPLAY. Loading from: {fixturePath}");
            // Replay Mode
            _isReplaying = true;
            _recordingState = new RecordingState(fixturePath, RecordingMode.Replay);
            // Rebuild provider for replay
            (_provider as IDisposable)?.Dispose();
            _provider = BuildServiceProvider(_recordingState);
        }
        else
        {
            Console.Error.WriteLine($"[VCR] Mode: SKIP. No token and no fixture found at {fixturePath}");
            // No token, no file -> Do nothing, RequireApi will skip
        }
    }

    private string GetFixturePath(string sourceFilePath, string testName)
    {
        var integrationDir = Path.GetDirectoryName(sourceFilePath);
        string baseDir;

        if (integrationDir != null && Directory.Exists(integrationDir))
        {
            baseDir = integrationDir; // Source checkout / local dev
        }
        else
        {
            // Fallback: Try to find repo root by walking up from CurrentDirectory
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            var repoRoot = current;
            while (repoRoot != null && !File.Exists(Path.Combine(repoRoot.FullName, "RaindropMcp.sln")))
            {
                repoRoot = repoRoot.Parent;
            }

            if (repoRoot != null)
            {
                // Found repo root, construct path to Integration tests
                baseDir = Path.Combine(repoRoot.FullName, "tests", "Mcp.Tests", "Integration");
                if (!Directory.Exists(baseDir))
                {
                    // If directory structure is different, fallback to CurrentDirectory
                     baseDir = Directory.GetCurrentDirectory();
                }
            }
            else
            {
                baseDir = Directory.GetCurrentDirectory();
            }
        }

        return Path.Combine(baseDir, "Fixtures", GetType().Name, $"{testName}.json");
    }

    protected string CurrentTestId
    {
        get
        {
            // If VCR is not initialized, fallback to random (e.g. strict Record mode or no-VCR tests)
            if (_recordingState == null) return Guid.NewGuid().ToString("N");

            const string key = "TestId";
            var existing = _recordingState.GetMetadata(key);
            if (existing != null) return existing;

            // Generate new
            var newId = Guid.NewGuid().ToString("N");
            _recordingState.SetMetadata(key, newId);
            return newId;
        }
    }

    public void Dispose()
    {
        _recordingState?.Save();
        (_provider as IDisposable)?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task PollUntilAsync(Func<Task<bool>> condition, string failureMessage, CancellationToken cancellationToken, int pollAttempts = 30, int pollIntervalMs = 1000)
    {
        for (var i = 0; i < pollAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await condition())
            {
                return; // Condition met
            }
            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        Assert.Fail(failureMessage);
    }
}
