namespace Mcp.Common;

/// <summary>
/// Base class for Raindrop API tools that simply stores the injected API
/// instance for use by derived classes.
/// </summary>
public abstract class RaindropToolBase<TApi>
{
    protected TApi Api { get; }

    protected RaindropToolBase(TApi api)
    {
        Api = api;
    }
}
