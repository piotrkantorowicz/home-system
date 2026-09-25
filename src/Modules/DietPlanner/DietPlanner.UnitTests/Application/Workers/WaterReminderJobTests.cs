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

/// <summary>Unit tests for <c>WaterReminderJob</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class WaterReminderJobTests
{
    private readonly IWaterReminderCandidateQueries _queries =
        Substitute.For<IWaterReminderCandidateQueries>();
    private readonly IWaterReminderStateRepository _stateRepo =
        Substitute.For<IWaterReminderStateRepository>();
    private readonly IHouseholdQueryService _persons = Substitute.For<IHouseholdQueryService>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly WaterReminderJob _sut;

    // 12:00 UTC — inside default window 06:00–22:00, interval=60 min
    private static readonly DateTime Now = new(2026, 4, 29, 12, 0, 0, DateTimeKind.Utc);

    private static WaterReminderCandidate MakeCandidate(
        Guid personId = default,
        int intervalMinutes = 60,
        string windowStart = "06:00",
        string windowEnd = "22:00",
        DateTime? lastAt = null)
        => new(personId == Guid.Empty ? Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458") : personId, "en", intervalMinutes,
            TimeOnly.Parse(windowStart, CultureInfo.InvariantCulture), TimeOnly.Parse(windowEnd, CultureInfo.InvariantCulture), lastAt);

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public WaterReminderJobTests()
    {
        _persons.GetAuthSubjectForPersonAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns("u1");
        _sut = new WaterReminderJob(_queries, _stateRepo, _bus, _uow, _persons);
    }

    /// <summary><c>Name</c> is stable.</summary>
    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("WaterReminderJob");

    /// <summary>When no candidates: <c>RunAsync</c> does nothing.</summary>
    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When outside time window: <c>RunAsync</c> skips candidate.</summary>
    [Fact]
    public async Task RunAsync_WhenOutsideTimeWindow_SkipsCandidate()
    {
        // Now = 12:00, window = 14:00–22:00 → outside
        var candidate = MakeCandidate(windowStart: "14:00", windowEnd: "22:00");
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When interval not yet passed: <c>RunAsync</c> skips candidate.</summary>
    [Fact]
    public async Task RunAsync_WhenIntervalNotYetPassed_SkipsCandidate()
    {
        // last reminder 30 min ago, interval = 60 min → too soon
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: Now.AddMinutes(-30));
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When first reminder ever: <c>RunAsync</c> publishes and creates new state.</summary>
    [Fact]
    public async Task RunAsync_WhenFirstReminderEver_PublishesAndCreatesNewState()
    {
        var candidate = MakeCandidate(lastAt: null);
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>()).Returns((WaterReminderState?)null);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<WaterReminderDueIntegrationEvent>(e =>
                e.UserId == "u1" && e.Locale == "en"),
            Arg.Any<CancellationToken>());

        await _stateRepo.Received(1).AddAsync(
            Arg.Is<WaterReminderState>(s => s.PersonId == Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458") && s.LastWaterReminderAt == Now),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When interval passed: <c>RunAsync</c> publishes and updates existing state.</summary>
    [Fact]
    public async Task RunAsync_WhenIntervalPassed_PublishesAndUpdatesExistingState()
    {
        var lastAt = Now.AddMinutes(-61);
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: lastAt);
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        var existing = WaterReminderState.Create(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), lastAt);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<WaterReminderDueIntegrationEvent>(e => e.UserId == "u1"),
            Arg.Any<CancellationToken>());

        await _stateRepo.DidNotReceive().AddAsync(Arg.Any<WaterReminderState>(), Arg.Any<CancellationToken>());
        existing.LastWaterReminderAt.ShouldBe(Now);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Exactly at interval boundary: <c>RunAsync</c> is eligible.</summary>
    [Fact]
    public async Task RunAsync_ExactlyAtIntervalBoundary_IsEligible()
    {
        // last at exactly Now - interval → eligible (boundary inclusive)
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: Now.AddMinutes(-60));
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);
        _stateRepo.GetByPersonIdAsync(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Arg.Any<CancellationToken>())
            .Returns(WaterReminderState.Create(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458"), Now.AddMinutes(-60)));

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Multiple eligible users: <c>RunAsync</c> commits once.</summary>
    [Fact]
    public async Task RunAsync_MultipleEligibleUsers_CommitsOnce()
    {
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>())
            .Returns([MakeCandidate(Guid.Parse("db6cd388-0abf-538a-8503-dd3358d93458")), MakeCandidate(Guid.Parse("256bfca0-761e-5058-8d12-d39fd1b216f2"))]);
        _stateRepo.GetByPersonIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WaterReminderState?)null);

        await _sut.RunAsync(Now, TestContext.Current.CancellationToken);

        await _bus.Received(2).PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
