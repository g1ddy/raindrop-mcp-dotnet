namespace Mcp.Common;

/// <summary>
/// Defines the standard CRUD-style operations for Raindrop APIs.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Refit", "RF001:Refit types must have Refit HTTP method attributes", Justification = "Generic interface")]
public interface ICommonApi<TEntity, TKey> where TEntity : class
{
    Task<ItemResponse<TEntity>> CreateAsync(TEntity entity, CancellationToken cancellationToken);
    Task<ItemResponse<TEntity>> GetAsync(TKey id, CancellationToken cancellationToken);
    Task<ItemResponse<TEntity>> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken);
    Task<SuccessResponse> DeleteAsync(TKey id, CancellationToken cancellationToken);
}
