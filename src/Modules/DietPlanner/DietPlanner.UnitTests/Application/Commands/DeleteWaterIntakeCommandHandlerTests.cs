namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.DeleteWaterIntake;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class DeleteWaterIntakeCommandHandlerTests
{
    private readonly IWaterIntakeRepository _repository = Substitute.For<IWaterIntakeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteWaterIntakeCommandHandler _sut;

    public DeleteWaterIntakeCommandHandlerTests()
        => _sut = new DeleteWaterIntakeCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WhenEntryExists_DeletesAndCommits()
    {
        var id = Guid.NewGuid();
        var intake = WaterIntake.Create(
            WaterIntakeId.From(id),
            "user-1",
            DateOnly.FromDateTime(DateTime.UtcNow),
            250,
            null);

        _repository.GetByIdAsync(WaterIntakeId.From(id), Arg.Any<CancellationToken>())
            .Returns(intake);

        await _sut.HandleAsync(new DeleteWaterIntakeCommand(id, "user-1"), CancellationToken.None);

        _repository.Received(1).Delete(intake);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntryNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(WaterIntakeId.From(id), Arg.Any<CancellationToken>())
            .Returns((WaterIntake?)null);

        var act = async () =>
            await _sut.HandleAsync(new DeleteWaterIntakeCommand(id, "user-1"), CancellationToken.None);

        await act.ShouldThrowAsync<NotFoundException>();
    }
}
