using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using ModelContextProtocol;
using Mcp.Common;

namespace Mcp.Raindrops;

using Microsoft.Extensions.Logging;

public class RaindropMonitorService : BackgroundService
{
    private readonly McpServer _server;
    private readonly IRaindropsApi _apiClient;
    private readonly ILogger<RaindropMonitorService> _logger;

    // First Boot Strategy: Look back exactly 10 minutes from startup
    private DateTime _newestBookmarkSeen = DateTime.UtcNow.AddMinutes(-10);

    public RaindropMonitorService(McpServer server, IRaindropsApi apiClient, ILogger<RaindropMonitorService> logger)
    {
        _server = server;
        _apiClient = apiClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kicks off the first check immediately upon startup without blocking the server handshake
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Fetch recent bookmarks from the API (collection 0 = all)
                // We'll get the first page of recent bookmarks, ordered by created desc
                var response = await _apiClient.ListAsync(0, null, "-created", 0, 50, true, stoppingToken);
                if (response?.Result == true && response.Items != null)
                {
                    var newBookmarks = new List<Raindrop>();

                    foreach (var bookmark in response.Items)
                    {
                        if (!bookmark.Created.HasValue) continue;
                        if (bookmark.Created.Value.ToUniversalTime() <= _newestBookmarkSeen) break;
                        newBookmarks.Add(bookmark);
                    }

                    if (newBookmarks.Any())
                    {
                        // Process oldest to newest so Claude reads them in chronological order
                        newBookmarks.Reverse();

                        foreach (var bookmark in newBookmarks)
                        {
                            // MUST be awaited. Synchronous writes can deadlock the Stdio stream.
                            await _server.SendNotificationAsync("notifications/claude/channel", new
                            {
                                content = $"New bookmark: {bookmark.Title}\nURL: {bookmark.Link}\nID: {bookmark.Id}",
                                meta = new { severity = "info" }
                            });
                        }

                        _newestBookmarkSeen = newBookmarks.Last().Created!.Value.ToUniversalTime();
                    }
                }
            }
            catch (Exception ex)
            {
                // Simple retry: let the loop naturally wait 10 mins
                _logger.LogError(ex, "[RaindropMonitor] Best-effort sync failed: {Message}", ex.Message);
            }

            // Wait 10 minutes before the next loop
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
