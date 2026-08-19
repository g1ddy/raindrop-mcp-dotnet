using ModelContextProtocol.Extensions.Tasks;

namespace Mcp.Analytics;

internal static class LibraryAnalyticsTaskPolicy
{
    public static McpTaskExecutionMode GetExecutionMode(string? toolName) =>
        toolName == LibraryAnalyticsTools.ToolName
            ? McpTaskExecutionMode.Optional
            : McpTaskExecutionMode.Synchronous;
}
