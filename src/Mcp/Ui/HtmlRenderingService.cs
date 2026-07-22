using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using Mcp.Raindrops;

namespace Mcp.Ui;

/// <summary>
/// Implementation of <see cref="IHtmlRenderingService"/> using <see cref="HtmlRenderer"/>.
/// </summary>
public class HtmlRenderingService : IHtmlRenderingService, IAsyncDisposable
{
    private readonly HtmlRenderer _htmlRenderer;

    public HtmlRenderingService(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);
    }

    public async Task<string> RenderBookmarksAsync(IEnumerable<Raindrop> bookmarks)
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { "Bookmarks", bookmarks }
        });

        // Use the renderer's Dispatcher.InvokeAsync to ensure thread safety
        var html = await _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await _htmlRenderer.RenderComponentAsync<Explorer>(parameters);
            return output.ToHtmlString();
        });

        return html;
    }

    public async ValueTask DisposeAsync()
    {
        await _htmlRenderer.DisposeAsync();
    }
}
