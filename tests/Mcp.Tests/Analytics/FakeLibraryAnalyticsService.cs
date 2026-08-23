using Mcp.Analytics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RaindropMcp.Tests.Analytics;

public sealed class FakeLibraryAnalyticsService : ILibraryAnalyticsService
{
    private TaskCompletionSource<LibraryAnalyticsReport>? _tcs;
    public CancellationToken ObservedCancellationToken { get; private set; }

    public TaskCompletionSource<bool> StartedTcs { get; } = new();

    public Task<LibraryAnalyticsReport> AnalyzeAsync(int collectionId, CancellationToken cancellationToken)
    {
        ObservedCancellationToken = cancellationToken;
        _tcs = new TaskCompletionSource<LibraryAnalyticsReport>(TaskCreationOptions.RunContinuationsAsynchronously);

        cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken));

        StartedTcs.TrySetResult(true);
        return _tcs.Task;
    }

    public void Complete(LibraryAnalyticsReport report)
    {
        _tcs?.TrySetResult(report);
    }

    public void Fail(Exception exception)
    {
        _tcs?.TrySetException(exception);
    }
}
