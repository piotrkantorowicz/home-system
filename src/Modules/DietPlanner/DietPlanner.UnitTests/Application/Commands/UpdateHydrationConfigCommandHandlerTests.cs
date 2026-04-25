namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.UpdateHydrationConfig;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class UpdateHydrationConfigCommandHandlerTests
{
    private readonly IHydrationConfigRepository _repository = Substitute.For<IHydrationConfigRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateHydrationConfigCommandHandler _sut;

    public UpdateHydrationConfigCommandHandlerTests()
        => _sut = new UpdateHydrationConfigCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WhenNoExistingConfig_CreatesNewConfig()
    {
        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((HydrationConfig?)null);

        var command = new UpdateHydrationConfigCommand("user-1", 3000, 300, false);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<HydrationConfig>(c =>
                c.UserId == "user-1" &&
                c.DailyWaterTargetMl == 3000 &&
                c.GlassSizeMl == 300 &&
                !c.TrackWaterIntake),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenConfigExists_UpdatesExistingConfig()
    {
        var existing = HydrationConfig.Create(HydrationConfigId.New(), "user-1", 2500, 250, true);
        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateHydrationConfigCommand("user-1", 3000, 300, false);

        await _sut.HandleAsync(command, CancellationToken.None);

        _repository.Received(1).Update(Arg.Is<HydrationConfig>(c =>
            c.DailyWaterTargetMl == 3000 &&
            c.GlassSizeMl == 300 &&
            !c.TrackWaterIntake));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
