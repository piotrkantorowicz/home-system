namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.BulkCompleteMealEntries;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>BulkCompleteMealEntriesCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class BulkCompleteMealEntriesCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkCompleteMealEntriesCommandHandler _sut;
    private static readonly DateOnly Today = TestClock.Today;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public BulkCompleteMealEntriesCommandHandlerTests()
        => _sut = new BulkCompleteMealEntriesCommandHandler(_repository, _unitOfWork, TestHouseholds.Solo());

    private static MealEntry NewEntry(MealEntryStatus status)
    {
        var entry = MealEntry.Create(MealEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today,
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null, TestClock.UtcNow);
        switch (status)
        {
            case MealEntryStatus.Done: entry.MarkDone(); break;
            case MealEntryStatus.Modified: entry.ApplyOverride(RecipeId.New(), []); break;
        }
        return entry;
    }

    /// <summary><c>HandleAsync</c> completes only planned entries and returns count.</summary>
    [Fact]
    public async Task HandleAsync_CompletesOnlyPlannedEntriesAndReturnsCount()
    {
        var planned1 = NewEntry(MealEntryStatus.Planned);
        var planned2 = NewEntry(MealEntryStatus.Planned);
        var done = NewEntry(MealEntryStatus.Done);
        var modified = NewEntry(MealEntryStatus.Modified);

        _repository.GetByPersonAndDateRangeAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, Today, Arg.Any<CancellationToken>())
            .Returns([planned1, planned2, done, modified]);

        var result = await _sut.HandleAsync(
            new BulkCompleteMealEntriesCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, "user-1"), TestContext.Current.CancellationToken);

        result.Completed.ShouldBe(2);
        planned1.Status.ShouldBe(MealEntryStatus.Done);
        planned2.Status.ShouldBe(MealEntryStatus.Done);
        modified.Status.ShouldBe(MealEntryStatus.Modified);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>With no planned entries: <c>HandleAsync</c> returns zero and does not commit.</summary>
    [Fact]
    public async Task HandleAsync_WithNoPlannedEntries_ReturnsZeroAndDoesNotCommit()
    {
        _repository.GetByPersonAndDateRangeAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, Today, Arg.Any<CancellationToken>())
            .Returns([NewEntry(MealEntryStatus.Done)]);

        var result = await _sut.HandleAsync(
            new BulkCompleteMealEntriesCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, "user-1"), TestContext.Current.CancellationToken);

        result.Completed.ShouldBe(0);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>Unit tests for <c>BulkCompleteMealEntriesCommandValidator</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class BulkCompleteMealEntriesCommandValidatorTests
{
    private readonly BulkCompleteMealEntriesCommandValidator _sut = new();

    /// <summary>With empty user id: <c>Validate</c> returns error.</summary>
    [Fact]
    public void Validate_WithEmptyPersonId_ReturnsError()
    {
        var today = TestClock.Today;

        var errors = _sut.Validate(new BulkCompleteMealEntriesCommand(Guid.Empty, today, "user-1")).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(BulkCompleteMealEntriesCommand.PersonId));
    }

    /// <summary><c>Validate</c> allows any date including future and avoids timezone false positives.</summary>
    [Fact]
    public void Validate_AllowsAnyDateIncludingFuture_AvoidsTimezoneFalsePositives()
    {
        // The validator deliberately does not restrict the date — server UTC vs the
        // user's local "today" can disagree across midnight, and the handler is safe
        // because it only transitions Planned entries that already exist on that date.
        var future = TestClock.Today.AddDays(7);

        var errors = _sut.Validate(new BulkCompleteMealEntriesCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), future, "user-1")).ToList();

        errors.ShouldBeEmpty();
    }
}
