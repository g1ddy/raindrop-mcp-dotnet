using Refit;
using Mcp.Common;

namespace Mcp.User;

public interface IUserApi
{
    [Get("/user")]
    Task<ItemResponse<UserInfo>> GetAsync();
}
