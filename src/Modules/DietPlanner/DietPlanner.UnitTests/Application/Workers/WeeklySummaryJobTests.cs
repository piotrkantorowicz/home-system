namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

public sealed class WeeklySummaryJobTests
{
    private readonly IWeeklySummaryCandidateQueries _queries =
        Substitute.For<IWeeklySummaryCandidateQueries>();
    private readonly IWeeklySummaryStateRepository _stateRepo =
        Substitute.For<IWeeklySummaryStateRepository>();
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
        string userId = "u1",
        DayOfWeek day = DayOfWeek.Sunday,
        string time = "08:00",
        DateTime? lastAt = null)
        => new(userId, "en", day, TimeOnly.Parse(time), lastAt);

    public WeeklySummaryJobTests()
        => _sut = new WeeklySummaryJob(_queries, _stateRepo, _bus, _uow);

    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("WeeklySummaryJob");

    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenWrongDayOfWeek_SkipsCandidate()
    {
        // Now is Sunday, candidate configured for Monday
        var candidate = MakeCandidate(day: DayOfWeek.Monday);
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenTimeNotYetReached_SkipsCandidate()
    {
        // Now is 08:15, candidate configured for 09:00
        var candidate = MakeCandidate(time: "09:00");
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenSentWithinLast7Days_SkipsCandidate()
    {
        // Last summary was 3 days ago — within dedup window
        var candidate = MakeCandidate(lastAt: Now.AddDays(-3));
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenFirstSummaryEver_PublishesAndCreatesNewState()
    {
        var candidate = MakeCandidate(lastAt: null);
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync("u1", Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns((WeeklySummaryState?)null);

        await _sut.RunAsync(Now, CancellationToken.None);

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
            Arg.Is<WeeklySummaryState>(s => s.UserId == "u1" && s.LastWeeklySummaryAt == Now),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenSevenDaysElapsed_PublishesAndUpdatesExistingState()
    {
        var lastAt = Now.AddDays(-7);
        var candidate = MakeCandidate(lastAt: lastAt);
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync("u1", Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);

        var existing = WeeklySummaryState.Create("u1", lastAt);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _stateRepo.DidNotReceive().AddAsync(Arg.Any<WeeklySummaryState>(), Arg.Any<CancellationToken>());
        existing.LastWeeklySummaryAt.ShouldBe(Now);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ExactlyAt7DayBoundary_IsEligible()
    {
        // Exactly 7 days ago → boundary inclusive
        var candidate = MakeCandidate(lastAt: Now.AddDays(-7));
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync("u1", Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns(WeeklySummaryState.Create("u1", Now.AddDays(-7)));

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ExactlyAtConfiguredTime_IsEligible()
    {
        // Now matches the configured time exactly
        var nowExact = new DateTime(2026, 4, 26, 8, 0, 0, DateTimeKind.Utc);
        var candidate = MakeCandidate(time: "08:00");
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>()).Returns([candidate]);
        _queries.GetStatsAsync("u1", Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns((WeeklySummaryState?)null);

        await _sut.RunAsync(nowExact, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_MultipleEligibleUsers_CommitsOnce()
    {
        _queries.GetCandidatesAsync(Arg.Any<CancellationToken>())
            .Returns([MakeCandidate("u1"), MakeCandidate("u2")]);
        _queries.GetStatsAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(DefaultStats);
        _stateRepo.GetByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((WeeklySummaryState?)null);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(2).PublishAsync(
            Arg.Any<WeeklySummaryDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
