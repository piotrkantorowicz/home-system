namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.OverrideMealEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>OverrideMealEntryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class OverrideMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OverrideMealEntryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public OverrideMealEntryCommandHandlerTests()
        => _sut = new OverrideMealEntryCommandHandler(
            _repository, _recipeRepository, _productRepository, _unitOfWork);

    private static MealEntry NewEntry(string userId = "user-1")
        => MealEntry.Create(MealEntryId.New(), userId, new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null, TestClock.UtcNow);

    /// <summary>With recipe only: <c>HandleAsync</c> overrides entry and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithRecipeOnly_OverridesEntryAndCommits()
    {
        var entry = NewEntry();
        var recipeIdGuid = Guid.NewGuid();
        var recipeId = RecipeId.From(recipeIdGuid);
        var recipe = Recipe.Create(recipeId, "Pizza", null, null, 1, null, "user-1", TestClock.UtcNow);

        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(recipe);

        var command = new OverrideMealEntryCommand(entry.Id.Value, "user-1", recipeIdGuid, []);
        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        entry.Status.ShouldBe(MealEntryStatus.Modified);
        entry.ActualRecipeId.ShouldBe(recipeId);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>With products only: <c>HandleAsync</c> overrides entry and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithProductsOnly_OverridesEntryAndCommits()
    {
        var entry = NewEntry();
        var productIdGuid = Guid.NewGuid();
        var productId = ProductId.From(productIdGuid);
        var product = Product.Create(
            productId, "Chocolate Bar",
            new NutritionPer100g(500m, 5m, 60m, 30m, 2m),
            "g", null, null, "user-1", TestClock.UtcNow);

        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _productRepository.GetByIdsAsync(
                Arg.Is<IReadOnlyCollection<ProductId>>(c => c.Contains(productId)),
                Arg.Any<CancellationToken>())
            .Returns([product]);

        var command = new OverrideMealEntryCommand(
            Id: entry.Id.Value,
            UserId: "user-1",
            ActualRecipeId: null,
            ActualProducts: [new ActualProductInput(productIdGuid, 50m, "g")]);
        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        entry.Status.ShouldBe(MealEntryStatus.Modified);
        entry.ActualProducts.Count.ShouldBe(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When recipe belongs to other user: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenRecipeBelongsToOtherUser_ThrowsNotFoundException()
    {
        var entry = NewEntry();
        var recipeIdGuid = Guid.NewGuid();
        var recipeId = RecipeId.From(recipeIdGuid);
        var foreignRecipe = Recipe.Create(recipeId, "Foreign", null, null, 1, null, "other-user", TestClock.UtcNow);

        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _recipeRepository.GetByIdAsync(recipeId, Arg.Any<CancellationToken>()).Returns(foreignRecipe);

        var command = new OverrideMealEntryCommand(entry.Id.Value, "user-1", recipeIdGuid, []);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(command, TestContext.Current.CancellationToken));
    }

    /// <summary>When product belongs to other user: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenProductBelongsToOtherUser_ThrowsNotFoundException()
    {
        var entry = NewEntry();
        var productIdGuid = Guid.NewGuid();
        var productId = ProductId.From(productIdGuid);
        var foreignProduct = Product.Create(
            productId, "Foreign Product",
            new NutritionPer100g(100m, 5m, 10m, 2m, 1m),
            "g", null, null, "other-user", TestClock.UtcNow);

        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _productRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<ProductId>>(),
                Arg.Any<CancellationToken>())
            .Returns([foreignProduct]);

        var command = new OverrideMealEntryCommand(entry.Id.Value, "user-1", null,
            [new ActualProductInput(productIdGuid, 50m, "g")]);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(command, TestContext.Current.CancellationToken));
    }
}
