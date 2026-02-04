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
    })
    { }

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
        Assert.Equal("en", user.Config.Lang);
        Assert.Equal(4, user.Config.FontSize);
        Assert.Equal(UserBrokenLevel.Basic, user.Config.BrokenLevel);
        Assert.Equal(UserFontColor.Sunset, user.Config.FontColor);
        Assert.Equal(UserRaindropsView.List, user.Config.RaindropsView);

        // Dropbox verification
        Assert.NotNull(user.Dropbox);
        Assert.True(user.Dropbox.Enabled, "Dropbox should be enabled");

        // Files verification
        Assert.NotNull(user.Files);
        Assert.Equal(1048576, user.Files.Size);
        Assert.Equal(1024, user.Files.Used);
        Assert.NotNull(user.Files.LastCheckPoint);
        Assert.Equal(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), user.Files.LastCheckPoint);
    }
}
