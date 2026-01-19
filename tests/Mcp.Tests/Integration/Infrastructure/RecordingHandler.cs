using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.Tests.Integration.Infrastructure;

public enum RecordingMode
{
    Record,
    Replay
}

public class RecordingHandler : DelegatingHandler
{
    private readonly string _filePath;
    private readonly RecordingMode _mode;
    private readonly TestScenario _scenario;
    private int _replayIndex = 0;
    private readonly object _lock = new();

    public RecordingHandler(string filePath, RecordingMode mode)
    {
        _filePath = filePath;
        _mode = mode;
        _scenario = new TestScenario();

        if (_mode == RecordingMode.Replay)
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _scenario = JsonSerializer.Deserialize<TestScenario>(json) ?? new TestScenario();
            }
            else
            {
                throw new FileNotFoundException($"Recording not found at {_filePath}");
            }
        }
    }

    public void SetMetadata(string key, string value)
    {
        lock (_lock)
        {
            _scenario.Metadata[key] = value;
        }
    }

    public string? GetMetadata(string key)
    {
        lock (_lock)
        {
            return _scenario.Metadata.TryGetValue(key, out var val) ? val : null;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_mode == RecordingMode.Replay)
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
        int index;
        RecordedInteraction interaction;

        lock (_lock)
        {
            index = _replayIndex++;
            if (index >= _scenario.Interactions.Count)
            {
                throw new InvalidOperationException($"No more recorded interactions. Requested: {request.Method} {request.RequestUri}");
            }
            interaction = _scenario.Interactions[index];
        }

        var response = new HttpResponseMessage((HttpStatusCode)interaction.StatusCode);
        if (interaction.ResponseBody != null)
        {
            response.Content = new StringContent(interaction.ResponseBody);
            // Restore content headers
            if (interaction.ResponseHeaders.TryGetValue("Content-Type", out var cts) && cts.Length > 0)
            {
                 if (System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(cts[0], out var mt))
                 {
                     response.Content.Headers.ContentType = mt;
                 }
            }
        }

        // Restore other headers
        foreach (var h in interaction.ResponseHeaders)
        {
            if (!h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) &&
                !h.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) // Content-Length is set by StringContent
            {
                 response.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
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
                 interaction.ResponseHeaders[h.Key] = h.Value.ToArray();
            }
        }

        lock (_lock)
        {
            _scenario.Interactions.Add(interaction);
        }

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

    public void Save()
    {
        if (_mode == RecordingMode.Record)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(_scenario, options);

            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(_filePath, json);
        }
    }
}
