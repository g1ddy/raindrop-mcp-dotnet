using System.ComponentModel;
using Mcp.Common;
using ModelContextProtocol.Server;

namespace Mcp.User;

[McpServerToolType]
public class UserTools(IUserApi api) : RaindropToolBase<IUserApi>(api)
{

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "Get User Info"),
     Description("Retrieves the details of the currently authenticated user.")]
    public Task<ItemResponse<UserInfo>> GetUserInfoAsync(CancellationToken cancellationToken) => Api.GetAsync(cancellationToken);
}
