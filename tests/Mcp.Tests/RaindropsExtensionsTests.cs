using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Common;
using Mcp.Raindrops;
using Moq;
using Xunit;

namespace Mcp.Tests;

public class RaindropsExtensionsTests
{
    [Fact]
    public async Task GetNewestSinceAsync_FiltersBookmarksCorrectly()
    {
        // Arrange
        var mockApi = new Mock<IRaindropsApi>();
        var since = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var bookmarks = new List<Raindrop>
        {
            new() { Id = 1, Created = since.AddMinutes(10) },
            new() { Id = 2, Created = since.AddMinutes(5) },
            new() { Id = 3, Created = since }, // Should be excluded (exclusive)
            new() { Id = 4, Created = since.AddMinutes(-5) } // Should be excluded
        };

        mockApi.Setup(api => api.ListAsync(0, null, "-created", 0, 50, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, bookmarks));

        // Act
        var result = await mockApi.Object.GetNewestSinceAsync(since);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.Id == 1);
        Assert.Contains(result, b => b.Id == 2);
        Assert.DoesNotContain(result, b => b.Id == 3);
        Assert.DoesNotContain(result, b => b.Id == 4);
    }

    [Fact]
    public async Task GetNewestSinceAsync_HandlesMultiplePages()
    {
        // Arrange
        var mockApi = new Mock<IRaindropsApi>();
        var since = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Page 0: all new
        var page0 = Enumerable.Range(1, 50).Select(i => new Raindrop
        {
            Id = i,
            Created = since.AddDays(1).AddMinutes(-i)
        }).ToList();

        // Page 1: some new, some old
        var page1 = new List<Raindrop>
        {
            new() { Id = 51, Created = since.AddMinutes(1) },
            new() { Id = 52, Created = since }, // Cutoff
            new() { Id = 53, Created = since.AddMinutes(-1) }
        };

        mockApi.Setup(api => api.ListAsync(0, null, "-created", 0, 50, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, page0));

        mockApi.Setup(api => api.ListAsync(0, null, "-created", 1, 50, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, page1));

        // Act
        var result = await mockApi.Object.GetNewestSinceAsync(since);

        // Assert
        Assert.Equal(51, result.Count);
        Assert.Contains(result, b => b.Id == 51);
        Assert.DoesNotContain(result, b => b.Id == 52);
    }
}
