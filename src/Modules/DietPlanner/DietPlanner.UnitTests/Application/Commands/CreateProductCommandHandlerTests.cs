namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateProduct;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;

public sealed class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductCommandHandler _sut;

    public CreateProductCommandHandlerTests()
    {
        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);
        _sut = new CreateProductCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsProductAndCommits()
    {
        var command = new CreateProductCommand("Chicken", 165m, 31m, 0m, 3.6m, 0m, "g", null, null, "user-1");

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Name == "Chicken" && p.CreatedByUserId == "user-1"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CreatedProductHasCorrectNutrition()
    {
        var command = new CreateProductCommand("Egg", 155m, 13m, 1.1m, 11m, 0m, "piece", null, 50m, "user-1");
        Product? captured = null;
        await _repository.AddAsync(Arg.Do<Product>(p => captured = p), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(command, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Nutrition.Calories.ShouldBe(155m);
        captured.Nutrition.Protein.ShouldBe(13m);
        captured.GramPerPiece.ShouldBe(50m);
    }
}
