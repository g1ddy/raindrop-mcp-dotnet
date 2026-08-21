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

    [Fact]
    public async Task SuggestAsync_UsesSemanticScoreWhenEmbeddingGeneratorIsPresent()
    {
        var development = new Collection { Id = 1, Title = "Dev" };
        var cooking = new Collection { Id = 2, Title = "Food" };

        var mockEmbeddings = new Mock<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
        mockEmbeddings.Setup(g => g.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<Microsoft.Extensions.AI.EmbeddingGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> inputs, Microsoft.Extensions.AI.EmbeddingGenerationOptions options, CancellationToken ct) =>
            {
                var inputList = inputs.ToList();
                var result = new Microsoft.Extensions.AI.GeneratedEmbeddings<Microsoft.Extensions.AI.Embedding<float>>();
                foreach(var input in inputList)
                {
                    // Dev match gets a vector similar to query, Food gets orthogonal
                    if (input.Contains("Food") || input.Contains("Pasta"))
                        result.Add(new Microsoft.Extensions.AI.Embedding<float>(new float[] { 0, 1 }));
                    else
                        result.Add(new Microsoft.Extensions.AI.Embedding<float>(new float[] { 1, 0 }));
                }
                return result;
            });

        var serviceWithEmbeddings = new CollectionSuggestionService(
            _api.Object,
            new CollectionSuggestionIndexCache(),
            Options.Create(new RaindropOptions { ApiToken = Guid.NewGuid().ToString() }),
            mockEmbeddings.Object);

        SetupLibrary(
        [
            new Raindrop
            {
                Id = 10, Title = ".NET DI",
                Collection = new IdRef { Id = development.Id }
            },
            new Raindrop
            {
                Id = 11, Title = "Pasta",
                Collection = new IdRef { Id = cooking.Id }
            }
        ]);

        var query = new Raindrop { Title = "C# programming" }; // completely different lexically

        var suggestions = await serviceWithEmbeddings.SuggestAsync(query, [development, cooking], 3, CancellationToken.None);

        Assert.Equal(development.Id, suggestions[0].Collection.Id);
        mockEmbeddings.Verify(g => g.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<Microsoft.Extensions.AI.EmbeddingGenerationOptions>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SuggestAsync_ExcludesQueriedBookmarkFromOwnSemanticCentroid()
    {
        var current = new Collection { Id = 1, Title = "Current" };
        var matching = new Collection { Id = 2, Title = "Matching" };
        var queriedBookmark = new Raindrop
        {
            Id = 10,
            Title = "Query",
            Collection = new IdRef { Id = current.Id }
        };

        SetupLibrary(
        [
            queriedBookmark,
            new Raindrop { Id = 20, Title = "Related", Collection = new IdRef { Id = matching.Id } }
        ]);

        var mockEmbeddings = new Mock<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
        mockEmbeddings
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Microsoft.Extensions.AI.EmbeddingGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> inputs, Microsoft.Extensions.AI.EmbeddingGenerationOptions _, CancellationToken _) =>
            {
                var embeddings = new Microsoft.Extensions.AI.GeneratedEmbeddings<Microsoft.Extensions.AI.Embedding<float>>();
                foreach (var input in inputs)
                {
                    embeddings.Add(new Microsoft.Extensions.AI.Embedding<float>(
                        input.Contains("Query") || input.Contains("Related")
                            ? new float[] { 1, 0 }
                            : new float[] { 0, 1 }));
                }
                return embeddings;
            });
        var service = new CollectionSuggestionService(
            _api.Object,
            new CollectionSuggestionIndexCache(),
            Options.Create(new RaindropOptions { ApiToken = Guid.NewGuid().ToString() }),
            mockEmbeddings.Object);

        var suggestions = await service.SuggestAsync(queriedBookmark, [current, matching], 2, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Equal(matching.Id, suggestions[0].Collection.Id);
    }

    [Fact]
    public async Task SuggestAsync_PropagatesCancellationFromQueryEmbeddingGeneration()
    {
        var collection = new Collection { Id = 1, Title = "Development" };
        SetupLibrary([]);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var mockEmbeddings = new Mock<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
        mockEmbeddings
            .Setup(generator => generator.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<Microsoft.Extensions.AI.EmbeddingGenerationOptions>(),
                cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));
        var service = new CollectionSuggestionService(
            _api.Object,
            new CollectionSuggestionIndexCache(),
            Options.Create(new RaindropOptions { ApiToken = Guid.NewGuid().ToString() }),
            mockEmbeddings.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SuggestAsync(
                new Raindrop { Title = "Query" },
                [collection],
                1,
                cancellationTokenSource.Token));
    }
}
