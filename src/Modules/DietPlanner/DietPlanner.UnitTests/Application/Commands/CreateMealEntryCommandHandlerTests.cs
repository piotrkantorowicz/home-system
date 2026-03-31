namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Domain;

public sealed class CreateMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateMealEntryCommandHandler _sut;

    public CreateMealEntryCommandHandlerTests()
        => _sut = new CreateMealEntryCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsMealEntryAndCommits()
    {
        var recipeId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 15);
        var command = new CreateMealEntryCommand("user-1", date, "Breakfast", recipeId, 1.5m, null, null, null);

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<MealEntry>(m => m.UserId == "user-1" && m.MealType == "Breakfast"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
