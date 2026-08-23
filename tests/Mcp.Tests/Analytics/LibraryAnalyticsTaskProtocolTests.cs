using Mcp;
using Mcp.Analytics;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Extensions.Tasks;
using System.Text.Json;
using Xunit;

namespace RaindropMcp.Tests.Analytics;

public class LibraryAnalyticsTaskProtocolTests
{
    [Fact]
    public async Task A_OptionalTaskNegotiation()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromSeconds(60));

        var result = await host.Client.CallToolAsTaskAsync(new CallToolRequestParams
        {
            Name = "analyze_library",
            Arguments = new Dictionary<string, JsonElement> { ["collectionId"] = JsonSerializer.SerializeToElement(0) }
        }, CancellationToken.None);

        Assert.True(result.IsTask);
        Assert.NotNull(result.TaskCreated);

        var started = await host.FakeAnalytics.StartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(started);
    }

    [Fact]
    public async Task B_SynchronousCompatibility()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromSeconds(60));

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var callTask = host.Client.CallToolAsync(
            "analyze_library",
            new Dictionary<string, object?> { ["collectionId"] = 0 },
            cancellationToken: cts.Token);

        await host.FakeAnalytics.StartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        host.FakeAnalytics.Complete(new LibraryAnalyticsReport
        {
            Scope = new LibraryAnalyticsScope { CollectionId = 0, IsComplete = true, PagesFetched = 1, StartedAt = DateTimeOffset.UtcNow, CompletedAt = DateTimeOffset.UtcNow, IncludesDescendants = true, TerminationReason = "completed" },
            Summary = new LibraryAnalyticsSummary { BookmarksAnalyzed = 1, RootCollections = 0, ChildCollections = 0, MaximumCollectionDepth = 0, UniqueDomains = 0, UniqueTags = 0, BookmarksWithoutDomains = 0, FavoriteBookmarks = 0, UntaggedBookmarks = 0, UnsortedBookmarks = 0, BookmarksWithoutExcerpts = 0 },
            Collections = new List<CollectionDistribution>(),
            Domains = new List<ValueDistribution>(),
            Tags = new List<ValueDistribution>(),
            Diagnostics = new List<string>()
        });

        var result = await callTask;

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task C_NonAnalyticsToolsRemainSynchronous()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromSeconds(60));

        var result = await host.Client.CallToolAsTaskAsync(new CallToolRequestParams
        {
            Name = "mock_synchronous_tool"
        }, CancellationToken.None);

        Assert.False(result.IsTask);
        Assert.Null(result.TaskCreated);
    }

    [Fact]
    public async Task D_SuccessfulTaskCompletion()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromSeconds(60));

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var tcs = new TaskCompletionSource<CallToolResult>();
        _ = Task.Run(async () =>
        {
            var result = await host.Client.CallToolWithPollingAsync(new CallToolRequestParams
            {
                Name = "analyze_library",
                Arguments = new Dictionary<string, JsonElement> { ["collectionId"] = JsonSerializer.SerializeToElement(0) }
            }, 100, cts.Token);
            tcs.TrySetResult(result);
        });

        await host.FakeAnalytics.StartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        host.FakeAnalytics.Complete(new LibraryAnalyticsReport
        {
            Scope = new LibraryAnalyticsScope { CollectionId = 0, IsComplete = true, PagesFetched = 1, StartedAt = DateTimeOffset.UtcNow, CompletedAt = DateTimeOffset.UtcNow, IncludesDescendants = true, TerminationReason = "completed" },
            Summary = new LibraryAnalyticsSummary { BookmarksAnalyzed = 1, RootCollections = 0, ChildCollections = 0, MaximumCollectionDepth = 0, UniqueDomains = 0, UniqueTags = 0, BookmarksWithoutDomains = 0, FavoriteBookmarks = 0, UntaggedBookmarks = 0, UnsortedBookmarks = 0, BookmarksWithoutExcerpts = 0 },
            Collections = new List<CollectionDistribution>(),
            Domains = new List<ValueDistribution>(),
            Tags = new List<ValueDistribution>(),
            Diagnostics = new List<string>()
        });

        var finalResult = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(finalResult);
        Assert.NotEmpty(finalResult.Content);
    }

    [Fact]
    public async Task E_CancellationPropagation()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromSeconds(60));

        var result = await host.Client.CallToolAsTaskAsync(new CallToolRequestParams
        {
            Name = "analyze_library",
            Arguments = new Dictionary<string, JsonElement> { ["collectionId"] = JsonSerializer.SerializeToElement(0) }
        }, CancellationToken.None);

        Assert.True(result.IsTask);
        Assert.NotNull(result.TaskCreated);

        await host.FakeAnalytics.StartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await host.Client.CancelTaskAsync(result.TaskCreated.TaskId, CancellationToken.None);

        await Task.Delay(100);

        Assert.True(host.FakeAnalytics.ObservedCancellationToken.IsCancellationRequested);

        var status = await host.Client.GetTaskAsync(result.TaskCreated.TaskId, CancellationToken.None);
        Assert.Equal(McpTaskStatus.Cancelled, status.Status);
    }

    [Fact]
    public async Task F_ToolException_CompletesTaskWithErrorToolResult()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromSeconds(60));

        var result = await host.Client.CallToolAsTaskAsync(new CallToolRequestParams
        {
            Name = "analyze_library",
            Arguments = new Dictionary<string, JsonElement> { ["collectionId"] = JsonSerializer.SerializeToElement(0) }
        }, CancellationToken.None);

        await host.FakeAnalytics.StartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        host.FakeAnalytics.Fail(new InvalidOperationException("Controlled error SECRET_SENTINEL"));

        await Task.Delay(100);

        var status = await host.Client.GetTaskAsync(result.TaskCreated!.TaskId, CancellationToken.None);

        var completed = Assert.IsType<CompletedTaskResult>(status);
        Assert.Equal(McpTaskStatus.Completed, completed.Status);

        var toolResult = completed.Result.Deserialize<CallToolResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(toolResult);
        Assert.True(toolResult.IsError);

        var content = Assert.Single(toolResult.Content);
        var textContent = Assert.IsType<TextContentBlock>(content);
        Assert.Contains("error", textContent.Text);
        Assert.DoesNotContain("SECRET_SENTINEL", textContent.Text);
        Assert.DoesNotContain("Controlled error", textContent.Text);
    }

    [Fact]
    public async Task G_TtlSmokeTest()
    {
        await using var host = await LibraryAnalyticsMcpTestHost.CreateAsync(TimeSpan.FromMilliseconds(50));

        var result = await host.Client.CallToolAsTaskAsync(new CallToolRequestParams
        {
            Name = "analyze_library",
            Arguments = new Dictionary<string, JsonElement> { ["collectionId"] = JsonSerializer.SerializeToElement(0) }
        }, CancellationToken.None);

        await host.FakeAnalytics.StartedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        host.FakeAnalytics.Complete(new LibraryAnalyticsReport
        {
            Scope = new LibraryAnalyticsScope { CollectionId = 0, IsComplete = true, PagesFetched = 1, StartedAt = DateTimeOffset.UtcNow, CompletedAt = DateTimeOffset.UtcNow, IncludesDescendants = true, TerminationReason = "completed" },
            Summary = new LibraryAnalyticsSummary { BookmarksAnalyzed = 1, RootCollections = 0, ChildCollections = 0, MaximumCollectionDepth = 0, UniqueDomains = 0, UniqueTags = 0, BookmarksWithoutDomains = 0, FavoriteBookmarks = 0, UntaggedBookmarks = 0, UnsortedBookmarks = 0, BookmarksWithoutExcerpts = 0 },
            Collections = new List<CollectionDistribution>(),
            Domains = new List<ValueDistribution>(),
            Tags = new List<ValueDistribution>(),
            Diagnostics = new List<string>()
        });

        await Task.Delay(200);

        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpProtocolException>(async () =>
        {
            await host.Client.GetTaskAsync(result.TaskCreated!.TaskId, CancellationToken.None);
        });

        Assert.Equal(ModelContextProtocol.McpErrorCode.InvalidParams, ex.ErrorCode);
    }
}
