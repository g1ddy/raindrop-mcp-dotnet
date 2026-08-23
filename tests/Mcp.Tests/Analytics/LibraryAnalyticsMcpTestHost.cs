using Mcp;
using Mcp.Analytics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Extensions.Tasks;
using System.Text.Json;
using System.IO.Pipelines;
using System.Text.Json.Nodes;

namespace RaindropMcp.Tests.Analytics;

public sealed class LibraryAnalyticsMcpTestHost : IAsyncDisposable
{
    public McpClient Client { get; private set; } = null!;
    public McpServer Server { get; }
    public FakeLibraryAnalyticsService FakeAnalytics { get; }
    public InMemoryMcpTaskStore TaskStore { get; }

    private readonly ServiceProvider _provider;
    private readonly CancellationTokenSource _cts;
    private readonly Task _serverTask;
    private readonly Pipe _clientToServer;
    private readonly Pipe _serverToClient;

    public static async Task<LibraryAnalyticsMcpTestHost> CreateAsync(TimeSpan taskTtl)
    {
        var host = new LibraryAnalyticsMcpTestHost(taskTtl);
        await host.InitializeAsync();
        return host;
    }

    private LibraryAnalyticsMcpTestHost(TimeSpan taskTtl)
    {
        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        _clientToServer = new Pipe();
        _serverToClient = new Pipe();

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Raindrop:ApiToken"] = "test-token",
                ["Analytics:MaxConcurrency"] = "1"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(b => b.ClearProviders());

        FakeAnalytics = new FakeLibraryAnalyticsService();
        services.AddSingleton<ILibraryAnalyticsService>(FakeAnalytics);

        TaskStore = new InMemoryMcpTaskStore
        {
            DefaultPollIntervalMs = 10,
            DefaultTimeToLive = taskTtl
        };

        services
            .AddMcpServer(options => {
                options.ServerInfo = new Implementation { Name = "test-server", Version = "1.0.0" };
                options.Capabilities = new ServerCapabilities
                {
                    Experimental = new Dictionary<string, object> { ["tasks"] = new JsonObject() }
                };
            })
            .WithStreamServerTransport(
                _clientToServer.Reader.AsStream(),
                _serverToClient.Writer.AsStream())
            .WithTools<LibraryAnalyticsTools>()
            .WithTasks(
                TaskStore,
                options => options.ExecutionModeSelector = request =>
                    LibraryAnalyticsTaskPolicy.GetExecutionMode(request.Params?.Name));

        _provider = services.BuildServiceProvider(validateScopes: true);

        Server = _provider.GetRequiredService<McpServer>();
        _serverTask = Server.RunAsync(_cts.Token);
    }

    private async Task InitializeAsync()
    {
        Client = await McpClient.CreateAsync(
            new StreamClientTransport(
                serverInput: _clientToServer.Writer.AsStream(),
                serverOutput: _serverToClient.Reader.AsStream()),
            new McpClientOptions
            {
                ClientInfo = new Implementation { Name = "TestClient", Version = "1.0.0" },
                Capabilities = new ClientCapabilities
                {
                    Experimental = new Dictionary<string, object> { ["tasks"] = new JsonObject() }
                }
            },
            cancellationToken: _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (Client != null) await Client.DisposeAsync();

        _cts.Cancel();

        _clientToServer.Writer.Complete();
        _serverToClient.Writer.Complete();

        try { await _serverTask; } catch { }

        _cts.Dispose();
        if (_provider != null) await _provider.DisposeAsync();
    }
}
