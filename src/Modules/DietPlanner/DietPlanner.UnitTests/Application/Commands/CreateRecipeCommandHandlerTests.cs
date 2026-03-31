namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Domain;

public sealed class CreateRecipeCommandHandlerTests
{
    private readonly IRecipeRepository _repository = Substitute.For<IRecipeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateRecipeCommandHandler _sut;

    public CreateRecipeCommandHandlerTests()
    {
        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);
        _sut = new CreateRecipeCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsRecipeAndCommits()
    {
        var ingredients = new List<CreateRecipeIngredientRequest>
        {
            new(Guid.NewGuid(), 200m, "g"),
            new(Guid.NewGuid(), 100m, "ml")
        };
        var command = new CreateRecipeCommand("Pasta Bolognese", "Classic pasta", null, 4, 30, ingredients, "user-1");

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<Recipe>(r => r.Name == "Pasta Bolognese" && r.Ingredients.Count == 2),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecipeHasIngredients()
    {
        var productId = Guid.NewGuid();
        var ingredients = new List<CreateRecipeIngredientRequest> { new(productId, 300m, "g") };
        var command = new CreateRecipeCommand("Salad", null, null, 1, null, ingredients, "user-1");
        Recipe? captured = null;
        await _repository.AddAsync(Arg.Do<Recipe>(r => captured = r), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(command, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Ingredients.ShouldHaveSingleItem();
        captured.Ingredients.First().ProductId.Value.ShouldBe(productId);
        captured.Ingredients.First().Amount.ShouldBe(300m);
    }
}
