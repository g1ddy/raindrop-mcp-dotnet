using System.ComponentModel;
using Microsoft.Extensions.Options;
using Mcp.Common;
using ModelContextProtocol.Server;

namespace Mcp.User;

[McpServerToolType]
public class UserTools(IUserApi api, RaindropCacheService cacheService, IOptions<RaindropOptions> options) : RaindropToolBase<IUserApi>(api)
{
    private readonly RaindropCacheService _cacheService = cacheService;
    private readonly string _cacheKey = options.Value.ApiToken;

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "Get User Info"),
     Description("Retrieves the details of the currently authenticated user.")]
    public Task<ItemResponse<UserInfo>> GetUserInfoAsync(CancellationToken cancellationToken)
        => _cacheService.GetUserInfoAsync(_cacheKey, async (ct) =>
        {
            var response = await Api.GetAsync(ct);
            return new ItemResponse<UserInfo>(response.Result, response.User);
        }, cancellationToken);
}
