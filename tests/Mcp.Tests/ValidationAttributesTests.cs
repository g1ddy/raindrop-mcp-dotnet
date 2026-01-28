using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mcp.Raindrops;
using Xunit;

namespace Mcp.Tests;

public class ValidationAttributesTests
{
    [Fact]
    public void RaindropCreateRequest_Link_HasRequiredAndUrlAttributes()
    {
        var property = typeof(RaindropCreateRequest).GetProperty(nameof(RaindropCreateRequest.Link));
        Assert.NotNull(property);

        Assert.NotNull(property.GetCustomAttribute<RequiredAttribute>());
        Assert.NotNull(property.GetCustomAttribute<UrlAttribute>());
    }

    [Fact]
    public void RaindropCreateRequest_Excerpt_HasMaxLengthAttribute()
    {
        var property = typeof(RaindropCreateRequest).GetProperty(nameof(RaindropCreateRequest.Excerpt));
        Assert.NotNull(property);

        var attr = property.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(10000, attr.Length);
    }

    [Fact]
    public void RaindropCreateRequest_Note_HasMaxLengthAttribute()
    {
        var property = typeof(RaindropCreateRequest).GetProperty(nameof(RaindropCreateRequest.Note));
        Assert.NotNull(property);

        var attr = property.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(10000, attr.Length);
    }

    [Fact]
    public void RaindropUpdateRequest_Link_HasUrlAttribute()
    {
        var property = typeof(RaindropUpdateRequest).GetProperty(nameof(RaindropUpdateRequest.Link));
        Assert.NotNull(property);

        Assert.NotNull(property.GetCustomAttribute<UrlAttribute>());
    }

    [Fact]
    public void RaindropUpdateRequest_Excerpt_HasMaxLengthAttribute()
    {
        var property = typeof(RaindropUpdateRequest).GetProperty(nameof(RaindropUpdateRequest.Excerpt));
        Assert.NotNull(property);

        var attr = property.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(10000, attr.Length);
    }

    [Fact]
    public void RaindropUpdateRequest_Note_HasMaxLengthAttribute()
    {
        var property = typeof(RaindropUpdateRequest).GetProperty(nameof(RaindropUpdateRequest.Note));
        Assert.NotNull(property);

        var attr = property.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(10000, attr.Length);
    }
}
