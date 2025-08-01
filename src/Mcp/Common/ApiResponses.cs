using System.ComponentModel;

namespace Mcp.Common;

[Description("Response containing a single item")]
public record ItemResponse<T>(bool Result, T Item);

[Description("Response containing a list of items")]
public record ItemsResponse<T>(bool Result, List<T> Items);

[Description("Response representing success or failure")]
public record SuccessResponse(bool Result);
