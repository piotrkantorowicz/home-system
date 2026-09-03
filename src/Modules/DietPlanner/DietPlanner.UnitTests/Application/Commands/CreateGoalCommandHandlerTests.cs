namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.CreateGoal;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;

public sealed class CreateGoalCommandHandlerTests
{
    private readonly IUserGoalRepository _repository = Substitute.For<IUserGoalRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateGoalCommandHandler _sut;

    public CreateGoalCommandHandlerTests()
        => _sut = new CreateGoalCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsGoalAndCommits()
    {
        var command = new CreateGoalCommand("user-1", 2000, 150m, 250m, 70m, 30m);

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<UserGoal>(g => g.UserId == "user-1" && g.DailyCalorieTarget == 2000),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

}
