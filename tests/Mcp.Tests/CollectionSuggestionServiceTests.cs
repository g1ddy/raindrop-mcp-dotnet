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

    [Fact]
    public async Task SuggestAsync_ExcludesQueriedBookmarkFromOwnTrainingDocument_DomainSignal()
    {
        var collection1 = new Collection { Id = 1, Title = "Collection One" };
        var collection2 = new Collection { Id = 2, Title = "Collection Two" };

        var queriedBookmark = new Raindrop
        {
            Id = 10,
            Domain = "unique-domain.com",
            Collection = new IdRef { Id = collection1.Id }
        };
        var otherBookmark = new Raindrop
        {
            Id = 20,
            Domain = "unique-domain.com",
            Collection = new IdRef { Id = collection2.Id }
        };

        SetupLibrary([queriedBookmark, otherBookmark]);

        var suggestions = await _service.SuggestAsync(queriedBookmark, [collection1, collection2], 2, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Equal(collection2.Id, suggestions[0].Collection.Id);
    }

    [Fact]
    public async Task SuggestAsync_ExcludesQueriedBookmarkFromOwnTrainingDocument_TagSignal()
    {
        var collection1 = new Collection { Id = 1, Title = "Collection One" };
        var collection2 = new Collection { Id = 2, Title = "Collection Two" };

        var queriedBookmark = new Raindrop
        {
            Id = 10,
            Tags = ["unique-tag"],
            Collection = new IdRef { Id = collection1.Id }
        };
        var otherBookmark = new Raindrop
        {
            Id = 20,
            Tags = ["unique-tag"],
            Collection = new IdRef { Id = collection2.Id }
        };

        SetupLibrary([queriedBookmark, otherBookmark]);

        var suggestions = await _service.SuggestAsync(queriedBookmark, [collection1, collection2], 2, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Equal(collection2.Id, suggestions[0].Collection.Id);
    }

    [Fact]
    public async Task SuggestAsync_ExcludesQueriedBookmarkFromOwnTrainingDocument_LexicalSignal()
    {
        var collection1 = new Collection { Id = 1, Title = "Alpha" };
        var collection2 = new Collection { Id = 2, Title = "Beta" };

        var queriedBookmark = new Raindrop
        {
            Id = 10,
            Title = "Quantum Computing Guide",
            Collection = new IdRef { Id = collection1.Id }
        };
        var otherBookmark = new Raindrop
        {
            Id = 20,
            Title = "Quantum Computing Basics",
            Collection = new IdRef { Id = collection2.Id }
        };

        SetupLibrary([queriedBookmark, otherBookmark]);

        var suggestions = await _service.SuggestAsync(queriedBookmark, [collection1, collection2], 2, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Equal(collection2.Id, suggestions[0].Collection.Id);
    }

    [Fact]
    public async Task SuggestAsync_DoesNotExcludeWhenBookmarkIdIsZeroOrDifferent()
    {
        var collection1 = new Collection { Id = 1, Title = "Alpha" };
        var collection2 = new Collection { Id = 2, Title = "Beta" };

        var storedBookmark = new Raindrop
        {
            Id = 10,
            Title = "Quantum Computing Guide",
            Collection = new IdRef { Id = collection1.Id }
        };

        SetupLibrary([storedBookmark]);

        var newBookmarkQuery = new Raindrop
        {
            Id = 0,
            Title = "Quantum Computing Guide"
        };

        var suggestions = await _service.SuggestAsync(newBookmarkQuery, [collection1, collection2], 2, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Equal(collection1.Id, suggestions[0].Collection.Id);
    }

    private void SetupLibrary(IReadOnlyList<Raindrop> bookmarks)
    {
        _api.Setup(api => api.ListAsync(0, null, null, 0, 50, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, bookmarks));
    }
}
