using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.Tests.Integration.Infrastructure;

public enum RecordingMode
{
    Record,
    Replay
}

public class RecordingState
{
    private readonly string _filePath;
    private readonly TestScenario _scenario;
    private int _replayIndex = 0;
    private readonly object _lock = new();

    public RecordingMode Mode { get; }

    public RecordingState(string filePath, RecordingMode mode)
    {
        _filePath = filePath;
        Mode = mode;
        _scenario = new TestScenario();

        if (Mode == RecordingMode.Replay)
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

    public RecordedInteraction GetNextInteraction(string method, string? uri)
    {
        lock (_lock)
        {
            int index = _replayIndex++;
            if (index >= _scenario.Interactions.Count)
            {
                throw new InvalidOperationException($"No more recorded interactions. Requested: {method} {uri}");
            }
            return _scenario.Interactions[index];
        }
    }

    public void AddInteraction(RecordedInteraction interaction)
    {
        lock (_lock)
        {
            _scenario.Interactions.Add(interaction);
        }
    }

    public void Save()
    {
        if (Mode == RecordingMode.Record)
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
