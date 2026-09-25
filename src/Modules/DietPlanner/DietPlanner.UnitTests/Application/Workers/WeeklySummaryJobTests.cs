namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
#pragma warning restore IDE0005
using System.Globalization;
using DietPlanner.Application.Workers;
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

/// <summary>Unit tests for <c>WeeklySummaryJob</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class WeeklySummaryJobTests
{
    private readonly IWeeklySummaryCandidateQueries _queries =
        Substitute.For<IWeeklySummaryCandidateQueries>();
    private readonly IWeeklySummaryStateRepository _stateRepo =
        Substitute.For<IWeeklySummaryStateRepository>();
    private readonly IHouseholdQueryService _persons = Substitute.For<IHouseholdQueryService>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly WeeklySummaryJob _sut;

    // Sunday 08:15 UTC — configured day=Sunday, time=08:00
    private static readonly DateTime Now = new(2026, 4, 26, 8, 15, 0, DateTimeKind.Utc);

    private static readonly WeeklyStats DefaultStats = new(
        TotalKcal: 12000,
        TargetKcal: 14000,
        AvgWaterLiters: 1.8m,
        WeightDeltaKg: -0.5m,
        MealsCompleted: 18,
        MealsPlanned: 21);

    private static WeeklySummaryCandidate MakeCandidate(
        Guid personId = default,
        DayOfWeek day = DayOfWeek.Sunday,
        string time = "08:00",
        DateTime? lastAt = null)
        => new(personId == Guid.Empty ? Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458") : personId, "en", day, TimeOnly.Parse(time, CultureInfo.InvariantCulture), lastAt);

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public WeeklySummaryJobTests()
    {
        _persons.GetAuthSubjectForPersonAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns("u1");
        _sut = new WeeklySummaryJob(_queries, _stateRepo, _bus, _uow, _persons);
    }

    /// <summary><c>Name</c> is stable.</summary>
    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("WeeklySummaryJob");

    /// <summary>When no candidates: <c>RunAsync</c> does nothing.</summary>
    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When wrong day of week: <c>RunAsync</c> skips candidate.</summary>
    [Fact]
    public async Task RunAsync_WhenWrongDayOfWeek_SkipsCandidate()
    {
        // Now is Sunday, candidate configured for Monday
        var candidate = MakeCandidate(day: DayOfWeek.Monday);
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When time not yet reached: <c>RunAsync</c> skips candidate.</summary>
    [Fact]
    public async Task RunAsync_WhenTimeNotYetReached_SkipsCandidate()
    {
        // Now is 08:15, candidate configured for 09:00
        var candidate = MakeCandidate(time: "09:00");
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When sent within last 7 days: <c>RunAsync</c> skips candidate.</summary>
    [Fact]
    public async Task RunAsync_WhenSentWithinLast7Days_SkipsCandidate()
    {
        // Last summary was 3 days ago — within dedup window
        var candidate = MakeCandidate(lastAt: Now.AddDays(-3));
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When first summary ever: <c>RunAsync</c> publishes and creates new state.</summary>
    [Fact]
    public async Task RunAsync_WhenFirstSummaryEver_PublishesAndCreatesNewState()
    {
        var candidate = MakeCandidate(lastAt: null);
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>())
            .Returns((WeeklySummaryState?)null);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        var expectedWeekEnd = DateOnly.FromDateTime(Now).AddDays(-1); // 2026-04-25
        var expectedWeekStart = expectedWeekEnd.AddDays(-6);            // 2026-04-19

        await _bus.Received(1).PublishAsync(
            Arg.Is<WeeklySummaryDueIntegrationEvent>(e =>
                e.UserId == "u1" &&
                e.Locale == "en" &&
                e.WeekStart == expectedWeekStart &&
                e.WeekEnd == expectedWeekEnd &&
                e.TotalKcal == 12000 &&
                e.TargetKcal == 14000 &&
                e.MealsCompleted == 18 &&
                e.MealsPlanned == 21),
            Arg.Any<CancellationToken>());

        await _stateRepo.Received(1).AddAsync(
            Arg.Is<WeeklySummaryState>(s => s.PersonId == Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458") && s.LastWeeklySummaryAt == Now),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When seven days elapsed: <c>RunAsync</c> publishes and updates existing state.</summary>
    [Fact]
    public async Task RunAsync_WhenSevenDaysElapsed_PublishesAndUpdatesExistingState()
    {
        var lastAt = Now.AddDays(-7);
        var candidate = MakeCandidate(lastAt: lastAt);
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);

        var existing = WeeklySummaryState.Create(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), lastAt);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _stateRepo.DidNotReceive().AddAsync(Arg.Any<WeeklySummaryState>(), Arg.Any<CancellationToken>());
        existing.LastWeeklySummaryAt.ShouldBe(Now);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Exactly at 7 day boundary: <c>RunAsync</c> is eligible.</summary>
    [Fact]
    public async Task RunAsync_ExactlyAt7DayBoundary_IsEligible()
    {
        // Exactly 7 days ago → boundary inclusive
        var candidate = MakeCandidate(lastAt: Now.AddDays(-7));
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>())
            .Returns(WeeklySummaryState.Create(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Now.AddDays(-7)));

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Exactly at configured time: <c>RunAsync</c> is eligible.</summary>
    [Fact]
    public async Task RunAsync_ExactlyAtConfiguredTime_IsEligible()
    {
        // Now matches the configured time exactly
        var nowExact = new DateTime(2026, 4, 26, 8, 0, 0, DateTimeKind.Utc);
        var candidate = MakeCandidate(time: "08:00");
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>())
            .Returns((WeeklySummaryState?)null);

        await _sut.RunAsync(nowExact, TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Multiple eligible users: <c>RunAsync</c> commits once.</summary>
    [Fact]
    public async Task RunAsync_MultipleEligibleUsers_CommitsOnce()
    {
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>())
            .Returns([MakeCandidate(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458")), MakeCandidate(Guid.Parse("256bfca0-761e-5058-8d12-d39fd1b216f2"))]);
        _queries.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByPersonIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WeeklySummaryState?)null);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(2).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
