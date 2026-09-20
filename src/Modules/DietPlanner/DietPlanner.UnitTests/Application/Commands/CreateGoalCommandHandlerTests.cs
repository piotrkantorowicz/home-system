namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.CreateGoal;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>CreateGoalCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class CreateGoalCommandHandlerTests
{
    private readonly IUserGoalRepository _repository = Substitute.For<IUserGoalRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly CreateGoalCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public CreateGoalCommandHandlerTests()
        => _sut = new CreateGoalCommandHandler(_repository, _unitOfWork, _clock);

    /// <summary>With valid command: <c>HandleAsync</c> adds goal and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsGoalAndCommits()
    {
        var command = new CreateGoalCommand("user-1", 2000, 150m, 250m, 70m, 30m);

        var id = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<UserGoal>(g => g.UserId == "user-1" && g.DailyCalorieTarget == 2000),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

}
