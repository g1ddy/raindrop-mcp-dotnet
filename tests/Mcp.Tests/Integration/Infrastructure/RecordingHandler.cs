using System.Net;

namespace Mcp.Tests.Integration.Infrastructure;

public class RecordingHandler : DelegatingHandler
{
    private readonly RecordingState _state;

    public RecordingHandler(RecordingState state)
    {
        _state = state;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_state.Mode == RecordingMode.Replay)
        {
            return await ReplayAsync(request, cancellationToken);
        }
        else
        {
            return await RecordAsync(request, cancellationToken);
        }
    }

    private Task<HttpResponseMessage> ReplayAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var interaction = _state.GetNextInteraction(request.Method.ToString(), request.RequestUri?.ToString());

        var response = new HttpResponseMessage((HttpStatusCode)interaction.StatusCode);
        if (interaction.ResponseBody != null)
        {
            response.Content = new StringContent(interaction.ResponseBody);

            // Restore content headers
            foreach (var h in interaction.ContentHeaders)
            {
                response.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
        }

        // Restore response headers
        foreach (var h in interaction.ResponseHeaders)
        {
            response.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        return Task.FromResult(response);
    }

    private async Task<HttpResponseMessage> RecordAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? requestBody = null;
        if (request.Content != null)
        {
            await request.Content.LoadIntoBufferAsync();
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Do not record 401s. This usually indicates a configuration error and recording it ruins the fixture.
            throw new InvalidOperationException($"Received 401 Unauthorized from {request.RequestUri}. Recording aborted to prevent saving invalid fixtures.");
        }

        var responseBody = response.Content != null ? await response.Content.ReadAsStringAsync(cancellationToken) : null;

        var interaction = new RecordedInteraction
        {
            Method = request.Method.ToString(),
            Uri = request.RequestUri?.ToString() ?? "",
            RequestBody = requestBody,
            StatusCode = (int)response.StatusCode,
            ResponseBody = responseBody
        };

        foreach (var h in response.Headers)
        {
            interaction.ResponseHeaders[h.Key] = h.Value.ToArray();
        }

        if (response.Content != null)
        {
            foreach (var h in response.Content.Headers)
            {
                interaction.ContentHeaders[h.Key] = h.Value.ToArray();
            }
        }

        _state.AddInteraction(interaction);

        // Replace the response content so it can be read again by the caller
        if (responseBody != null)
        {
            var newContent = new StringContent(responseBody);
            if (response.Content?.Headers.ContentType != null)
            {
                newContent.Headers.ContentType = response.Content.Headers.ContentType;
            }
            response.Content = newContent;
        }

        return response;
    }
}
