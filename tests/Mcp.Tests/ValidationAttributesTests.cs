using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mcp.Raindrops;
using Xunit;

namespace Mcp.Tests;

public class ValidationAttributesTests
{
    [Theory]
    [InlineData(typeof(RaindropCreateRequest), nameof(RaindropCreateRequest.Link))]
    public void Property_HasRequiredAttribute(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.NotNull(property.GetCustomAttribute<RequiredAttribute>());
    }

    [Theory]
    [InlineData(typeof(RaindropCreateRequest), nameof(RaindropCreateRequest.Link))]
    [InlineData(typeof(RaindropUpdateRequest), nameof(RaindropUpdateRequest.Link))]
    [InlineData(typeof(Raindrop), nameof(Raindrop.Link))]
    public void Property_HasUrlAttribute(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.NotNull(property.GetCustomAttribute<UrlAttribute>());
    }

    [Theory]
    [InlineData(typeof(RaindropCreateRequest), nameof(RaindropCreateRequest.Excerpt), 10000)]
    [InlineData(typeof(RaindropCreateRequest), nameof(RaindropCreateRequest.Note), 10000)]
    [InlineData(typeof(RaindropUpdateRequest), nameof(RaindropUpdateRequest.Excerpt), 10000)]
    [InlineData(typeof(RaindropUpdateRequest), nameof(RaindropUpdateRequest.Note), 10000)]
    [InlineData(typeof(Raindrop), nameof(Raindrop.Excerpt), 10000)]
    [InlineData(typeof(Raindrop), nameof(Raindrop.Note), 10000)]
    public void Property_HasMaxLengthAttribute(Type type, string propertyName, int expectedLength)
    {
        var property = type.GetProperty(propertyName);
        Assert.NotNull(property);

        var attr = property.GetCustomAttribute<MaxLengthAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedLength, attr.Length);
    }
}
