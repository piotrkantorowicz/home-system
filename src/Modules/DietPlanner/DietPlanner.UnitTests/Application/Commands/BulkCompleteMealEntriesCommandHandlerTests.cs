namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005
using DietPlanner.Application.Commands.BulkCompleteMealEntries;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class BulkCompleteMealEntriesCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkCompleteMealEntriesCommandHandler _sut;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public BulkCompleteMealEntriesCommandHandlerTests()
        => _sut = new BulkCompleteMealEntriesCommandHandler(_repository, _unitOfWork);

    private static MealEntry NewEntry(MealEntryStatus status)
    {
        var entry = MealEntry.Create(MealEntryId.New(), "user-1", Today,
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null);
        switch (status)
        {
            case MealEntryStatus.Done: entry.MarkDone(); break;
            case MealEntryStatus.Modified: entry.ApplyOverride(RecipeId.New(), []); break;
        }
        return entry;
    }

    [Fact]
    public async Task HandleAsync_CompletesOnlyPlannedEntriesAndReturnsCount()
    {
        var planned1 = NewEntry(MealEntryStatus.Planned);
        var planned2 = NewEntry(MealEntryStatus.Planned);
        var done = NewEntry(MealEntryStatus.Done);
        var modified = NewEntry(MealEntryStatus.Modified);

        _repository.GetByUserAndDateRangeAsync("user-1", Today, Today, Arg.Any<CancellationToken>())
            .Returns([planned1, planned2, done, modified]);

        var result = await _sut.HandleAsync(
            new BulkCompleteMealEntriesCommand("user-1", Today), CancellationToken.None);

        result.Completed.ShouldBe(2);
        planned1.Status.ShouldBe(MealEntryStatus.Done);
        planned2.Status.ShouldBe(MealEntryStatus.Done);
        modified.Status.ShouldBe(MealEntryStatus.Modified);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNoPlannedEntries_ReturnsZeroAndDoesNotCommit()
    {
        _repository.GetByUserAndDateRangeAsync("user-1", Today, Today, Arg.Any<CancellationToken>())
            .Returns([NewEntry(MealEntryStatus.Done)]);

        var result = await _sut.HandleAsync(
            new BulkCompleteMealEntriesCommand("user-1", Today), CancellationToken.None);

        result.Completed.ShouldBe(0);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class BulkCompleteMealEntriesCommandValidatorTests
{
    private readonly BulkCompleteMealEntriesCommandValidator _sut = new();

    [Fact]
    public void Validate_WithFutureDate_ReturnsError()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var errors = _sut.Validate(new BulkCompleteMealEntriesCommand("user-1", future)).ToList();

        errors.ShouldContain(e => e.PropertyName == "Date");
    }

    [Fact]
    public void Validate_WithTodayOrPast_ReturnsNoError()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var errors = _sut.Validate(new BulkCompleteMealEntriesCommand("user-1", today)).ToList();

        errors.ShouldBeEmpty();
    }
}
