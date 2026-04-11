using System.ComponentModel;
using Microsoft.Extensions.Options;
using Mcp.Common;
using ModelContextProtocol.Server;

namespace Mcp.User;

[McpServerToolType]
public class UserTools : RaindropToolBase<IUserApi>
{
    private readonly IRaindropCacheService _cacheService;
    private readonly string _cacheKey;
    private readonly Func<CancellationToken, Task<ItemResponse<UserInfo>>> _fetchFunc;

    public UserTools(IUserApi api, IRaindropCacheService cacheService, IOptions<RaindropOptions> options) : base(api)
    {
        _cacheService = cacheService;
        _cacheKey = options.Value.ApiToken;
        _fetchFunc = async ct =>
        {
            var response = await Api.GetAsync(ct);
            return new ItemResponse<UserInfo>(response.Result, response.User);
        };
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "Get User Info"),
     Description("Retrieves the details of the currently authenticated user.")]
    public Task<ItemResponse<UserInfo>> GetUserInfoAsync(CancellationToken cancellationToken)
        => _cacheService.GetUserInfoAsync(_cacheKey, _fetchFunc, cancellationToken);
}
