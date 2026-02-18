using BenchmarkDotNet.Attributes;
using Mcp.User;
using Mcp.Common;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Mcp.Benchmarks;

[MemoryDiagnoser]
public class UserToolsBenchmark
{
    private UserTools _tools;
    private Mock<IUserApi> _userApiMock;

    [GlobalSetup]
    public void Setup()
    {
        _userApiMock = new Mock<IUserApi>();

        // Mock GetAsync with a slight delay to simulate network latency
        _userApiMock.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => {
                await Task.Delay(50, ct);
                return new UserResponse(true, new UserInfo { Id = 123, Email = "test@example.com" });
            });

        var options = Options.Create(new RaindropOptions { ApiToken = "benchmark-token" });
        _tools = new UserTools(_userApiMock.Object, new RaindropCacheService(), options);
    }

    [Benchmark]
    public async Task GetUserInfo()
    {
        await _tools.GetUserInfoAsync(CancellationToken.None);
    }
}
