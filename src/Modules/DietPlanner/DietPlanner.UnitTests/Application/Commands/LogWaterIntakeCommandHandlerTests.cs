namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.LogWaterIntake;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;

public sealed class LogWaterIntakeCommandHandlerTests
{
    private readonly IWaterIntakeRepository _repository = Substitute.For<IWaterIntakeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LogWaterIntakeCommandHandler _sut;

    public LogWaterIntakeCommandHandlerTests()
        => _sut = new LogWaterIntakeCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsEntryAndCommits()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var command = new LogWaterIntakeCommand("user-1", date, 250, "Morning");

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<WaterIntake>(e =>
                e.UserId == "user-1" &&
                e.AmountMl == 250 &&
                e.Note == "Morning"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNullNote_AddsEntryAndCommits()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var command = new LogWaterIntakeCommand("user-1", date, 500, null);

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<WaterIntake>(e => e.Note == null),
            Arg.Any<CancellationToken>());
    }
}
