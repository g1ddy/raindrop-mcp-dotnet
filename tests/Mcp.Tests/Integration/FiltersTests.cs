using Mcp.Filters;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class FiltersTests : TestBase
{
    public FiltersTests() : base(s => s.AddTransient<FiltersTools>()) { }

    [SkippableFact]
    public async Task List()
    {
        RequireApi();
        var filters = Provider.GetRequiredService<FiltersTools>();
        var response = await filters.GetAvailableFiltersAsync(0);
        Assert.True(response.Result);
    }
}
