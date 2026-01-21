using Mcp.User;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class UserTests : TestBase
{
    public UserTests() : base(s =>
    {
        s.AddTransient<UserTools>();
    }) { }

    [SkippableFact]
    public async Task GetUserInfoAsync_ReturnsValidInfo()
    {
        var (cts, cancellationToken, uniqueId) = SetupTestForVcr(TimeSpan.FromSeconds(30));
        using var _ = cts;

        var userTools = Provider.GetRequiredService<UserTools>();

        var response = await userTools.GetUserInfoAsync(cancellationToken);

        Assert.NotNull(response);
        Assert.NotNull(response.Item);

        var user = response.Item;
        Assert.True(user.Id > 0, "User ID should be positive");
        Assert.False(string.IsNullOrEmpty(user.Email), "Email should not be empty");
        Assert.False(string.IsNullOrEmpty(user.FullName), "FullName should not be empty");

        // Config verification
        Assert.NotNull(user.Config);
        Assert.False(string.IsNullOrEmpty(user.Config.Lang), "Config.Lang should not be empty");

        // Dropbox verification
        Assert.NotNull(user.Dropbox);

        // Files verification
        Assert.NotNull(user.Files);
        Assert.True(user.Files.Size >= 0, "Files.Size should be non-negative");
    }
}
