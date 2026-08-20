using Mcp.Collections;
using Mcp.Collections.Suggestions;
using Mcp.Common;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;
using Moq;

namespace Mcp.Tests;

public class CollectionSuggestionServiceTests
{
    private readonly Mock<IRaindropsApi> _api = new();
    private readonly CollectionSuggestionService _service;

    public CollectionSuggestionServiceTests()
    {
        _service = new CollectionSuggestionService(
            _api.Object,
            new CollectionSuggestionIndexCache(),
            Options.Create(new RaindropOptions { ApiToken = Guid.NewGuid().ToString() }));
    }

    [Fact]
    public async Task SuggestAsync_RanksUsingLexicalTagAndDomainSignals()
    {
        var development = new Collection { Id = 1, Title = "Software Development", Description = "Programming resources" };
        var cooking = new Collection { Id = 2, Title = "Cooking", Description = "Food and recipes" };
        SetupLibrary(
        [
            new Raindrop
            {
                Id = 10, Link = "https://learn.microsoft.com/dotnet", Title = ".NET dependency injection",
                Excerpt = "C# programming guide", Tags = ["dotnet", "code"], Domain = "learn.microsoft.com",
                Collection = new IdRef { Id = development.Id }
            },
            new Raindrop
            {
                Id = 11, Link = "https://food.example/pasta", Title = "Pasta recipe",
                Tags = ["recipe"], Domain = "food.example", Collection = new IdRef { Id = cooking.Id }
            }
        ]);
        var query = new Raindrop
        {
            Link = "https://learn.microsoft.com/aspnet",
            Title = "ASP.NET programming",
            Excerpt = "A .NET development guide",
            Tags = ["dotnet"],
            Domain = "learn.microsoft.com"
        };

        var suggestions = await _service.SuggestAsync(query, [development, cooking], 3, CancellationToken.None);

        Assert.Equal(development.Id, suggestions[0].Collection.Id);
        Assert.DoesNotContain(suggestions, suggestion => suggestion.Collection.Id == cooking.Id);
    }

    [Fact]
    public async Task SuggestAsync_UsesCollectionMetadataForColdStartCollection()
    {
        var research = new Collection { Id = 1, Title = "Machine Learning Research", Description = "Papers and experiments" };
        var travel = new Collection { Id = 2, Title = "Travel", Description = "Trips and hotels" };
        SetupLibrary([]);
        var query = new Raindrop
        {
            Link = "https://arxiv.org/paper",
            Title = "Machine learning research paper",
            Excerpt = "New experimental results"
        };

        var suggestions = await _service.SuggestAsync(query, [research, travel], 3, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Equal(research.Id, suggestions[0].Collection.Id);
    }

    [Fact]
    public async Task SuggestAsync_ReturnsStableCollectionIdOrderForTies()
    {
        var first = new Collection { Id = 1, Title = "Dotnet" };
        var second = new Collection { Id = 2, Title = "Dotnet" };
        SetupLibrary([]);
        var query = new Raindrop { Link = "https://example.com", Title = "Dotnet" };

        var suggestions = await _service.SuggestAsync(query, [second, first], 2, CancellationToken.None);

        Assert.Equal([1, 2], suggestions.Select(suggestion => suggestion.Collection.Id));
    }

    [Fact]
    public async Task Invalidate_RebuildsTheHistoricalIndex()
    {
        var collection = new Collection { Id = 1, Title = "Development" };
        SetupLibrary([new Raindrop { Id = 1, Link = "https://example.com", Title = "Rust", Collection = new IdRef { Id = 1 } }]);
        var query = new Raindrop { Link = "https://example.com", Title = "Rust" };
        await _service.SuggestAsync(query, [collection], 1, CancellationToken.None);

        _service.Invalidate();
        await _service.SuggestAsync(query, [collection], 1, CancellationToken.None);

        _api.Verify(api => api.ListAsync(0, null, null, 0, 50, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SuggestAsync_DoesNotCacheFailedIndexBuild()
    {
        var collection = new Collection { Id = 1, Title = "Development" };
        _api.SetupSequence(api => api.ListAsync(0, null, null, 0, 50, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(false, []))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, []));
        var query = new Raindrop { Link = "https://example.com", Title = "Development" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SuggestAsync(query, [collection], 1, CancellationToken.None));
        var suggestions = await _service.SuggestAsync(query, [collection], 1, CancellationToken.None);

        Assert.Single(suggestions);
        _api.Verify(api => api.ListAsync(0, null, null, 0, 50, true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private void SetupLibrary(IReadOnlyList<Raindrop> bookmarks)
    {
        _api.Setup(api => api.ListAsync(0, null, null, 0, 50, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, bookmarks));
    }
}
