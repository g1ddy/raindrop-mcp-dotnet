using Mcp.Analytics;
using ModelContextProtocol.Extensions.Tasks;

namespace RaindropMcp.Tests.Analytics;

public class LibraryAnalyticsTaskPolicyTests
{
    [Fact]
    public void AnalyzeLibrarySupportsOptionalTaskExecution()
    {
        Assert.Equal(
            McpTaskExecutionMode.Optional,
            LibraryAnalyticsTaskPolicy.GetExecutionMode(LibraryAnalyticsTools.ToolName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("list_bookmarks")]
    public void OtherToolsRemainSynchronous(string? toolName)
    {
        Assert.Equal(
            McpTaskExecutionMode.Synchronous,
            LibraryAnalyticsTaskPolicy.GetExecutionMode(toolName));
    }
}
