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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cts.Token;

        var filters = Provider.GetRequiredService<FiltersTools>();
        var response = await filters.GetAvailableFiltersAsync(0, cancellationToken: cancellationToken);

        Assert.True(response.Result);
        Assert.NotNull(response.Tags);
        Assert.NotNull(response.Types);
    }
}
