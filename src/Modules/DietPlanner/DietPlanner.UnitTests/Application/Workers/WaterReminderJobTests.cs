namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
#pragma warning restore IDE0005
using System.Globalization;
using DietPlanner.Application.Workers;
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

public sealed class WaterReminderJobTests
{
    private readonly IWaterReminderCandidateQueries _queries =
        Substitute.For<IWaterReminderCandidateQueries>();
    private readonly IWaterReminderStateRepository _stateRepo =
        Substitute.For<IWaterReminderStateRepository>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly WaterReminderJob _sut;

    // 12:00 UTC — inside default window 06:00–22:00, interval=60 min
    private static readonly DateTime Now = new(2026, 4, 29, 12, 0, 0, DateTimeKind.Utc);

    private static WaterReminderCandidate MakeCandidate(
        string userId = "u1",
        int intervalMinutes = 60,
        string windowStart = "06:00",
        string windowEnd = "22:00",
        DateTime? lastAt = null)
        => new(userId, "en", intervalMinutes,
            TimeOnly.Parse(windowStart, CultureInfo.InvariantCulture), TimeOnly.Parse(windowEnd, CultureInfo.InvariantCulture), lastAt);

    public WaterReminderJobTests()
        => _sut = new WaterReminderJob(_queries, _stateRepo, _bus, _uow);

    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("WaterReminderJob");

    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenOutsideTimeWindow_SkipsCandidate()
    {
        // Now = 12:00, window = 14:00–22:00 → outside
        var candidate = MakeCandidate(windowStart: "14:00", windowEnd: "22:00");
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenIntervalNotYetPassed_SkipsCandidate()
    {
        // last reminder 30 min ago, interval = 60 min → too soon
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: Now.AddMinutes(-30));
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenFirstReminderEver_PublishesAndCreatesNewState()
    {
        var candidate = MakeCandidate(lastAt: null);
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns((WaterReminderState?)null);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<WaterReminderDueIntegrationEvent>(e =>
                e.UserId == "u1" && e.Locale == "en"),
            Arg.Any<CancellationToken>());

        await _stateRepo.Received(1).AddAsync(
            Arg.Is<WaterReminderState>(s => s.UserId == "u1" && s.LastWaterReminderAt == Now),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenIntervalPassed_PublishesAndUpdatesExistingState()
    {
        var lastAt = Now.AddMinutes(-61);
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: lastAt);
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        var existing = WaterReminderState.Create("u1", lastAt);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<WaterReminderDueIntegrationEvent>(e => e.UserId == "u1"),
            Arg.Any<CancellationToken>());

        await _stateRepo.DidNotReceive().AddAsync(Arg.Any<WaterReminderState>(), Arg.Any<CancellationToken>());
        existing.LastWaterReminderAt.ShouldBe(Now);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ExactlyAtIntervalBoundary_IsEligible()
    {
        // last at exactly Now - interval → eligible (boundary inclusive)
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: Now.AddMinutes(-60));
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns(WaterReminderState.Create("u1", Now.AddMinutes(-60)));

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_MultipleEligibleUsers_CommitsOnce()
    {
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>())
            .Returns([MakeCandidate("u1"), MakeCandidate("u2")]);
        _stateRepo.GetByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((WaterReminderState?)null);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(2).PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
