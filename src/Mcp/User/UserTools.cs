using System.ComponentModel;
using Mcp.Common;
using ModelContextProtocol.Server;

namespace Mcp.User;

[McpServerToolType]
public class UserTools(IUserApi api, RaindropCacheService cacheService) : RaindropToolBase<IUserApi>(api)
{
    private readonly RaindropCacheService _cacheService = cacheService;

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "Get User Info"),
     Description("Retrieves the details of the currently authenticated user.")]
    public Task<ItemResponse<UserInfo>> GetUserInfoAsync(CancellationToken cancellationToken)
        => _cacheService.GetUserInfoAsync(async (ct) =>
        {
            var response = await Api.GetAsync(ct);
            return new ItemResponse<UserInfo>(response.Result, response.User);
        }, cancellationToken);
}
