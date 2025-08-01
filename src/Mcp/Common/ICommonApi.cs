namespace Mcp.Common;

/// <summary>
/// Defines the standard CRUD-style operations for Raindrop APIs.
/// </summary>
public interface ICommonApi<TEntity, TKey>
{
    Task<ItemResponse<TEntity>> CreateAsync(TEntity entity);
    Task<ItemResponse<TEntity>> GetAsync(TKey id);
    Task<ItemResponse<TEntity>> UpdateAsync(TKey id, TEntity entity);
    Task<SuccessResponse> DeleteAsync(TKey id);
}
