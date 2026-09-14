namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

/// <summary>Unit tests for <c>MealReminderJob</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class MealReminderJobTests
{
    private readonly IMealReminderCandidateQueries _queries = Substitute.For<IMealReminderCandidateQueries>();
    private readonly ISentMealReminderRepository _ledger = Substitute.For<ISentMealReminderRepository>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly MealReminderJob _sut;
    private static readonly DateTime Now = new(2026, 4, 28, 11, 30, 0, DateTimeKind.Utc);

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public MealReminderJobTests()
        => _sut = new MealReminderJob(_queries, _ledger, _bus, _uow);

    /// <summary><c>Name</c> is stable.</summary>
    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("MealReminderJob");

    /// <summary>When no candidates: <c>RunAsync</c> does nothing.</summary>
    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(Arg.Any<MealReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _bus.DidNotReceive().PublishAsync(Arg.Any<MealMissedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().AddAsync(Arg.Any<SentMealReminder>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary><c>RunAsync</c> publishes reminder event and writes ledger and per due candidate.</summary>
    [Fact]
    public async Task RunAsync_PublishesReminderEventAndWritesLedger_PerDueCandidate()
    {
        var entryId = Guid.NewGuid();
        var planned = Now.AddMinutes(10);
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", entryId, "Lunch", planned)]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _ledger.ExistsAsync(Arg.Any<MealEntryId>(), MealReminderKind.Reminder, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MealReminderDueIntegrationEvent>(e =>
                e.UserId == "u1" && e.Locale == "en" &&
                e.MealEntryId == entryId && e.MealSlotName == "Lunch" &&
                e.PlannedAt == planned),
            Arg.Any<CancellationToken>());

        await _ledger.Received(1).AddAsync(
            Arg.Is<SentMealReminder>(l =>
                l.MealEntryId.Value == entryId && l.Kind == MealReminderKind.Reminder),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary><c>RunAsync</c> publishes missed event and writes ledger and per missed candidate.</summary>
    [Fact]
    public async Task RunAsync_PublishesMissedEventAndWritesLedger_PerMissedCandidate()
    {
        var entryId = Guid.NewGuid();
        var planned = Now.AddHours(-2);
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", entryId, "Breakfast", planned)]);
        _ledger.ExistsAsync(Arg.Any<MealEntryId>(), MealReminderKind.Missed, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MealMissedIntegrationEvent>(e =>
                e.MealEntryId == entryId && e.MealSlotName == "Breakfast" && e.PlannedAt == planned),
            Arg.Any<CancellationToken>());

        await _ledger.Received(1).AddAsync(
            Arg.Is<SentMealReminder>(l => l.Kind == MealReminderKind.Missed),
            Arg.Any<CancellationToken>());
    }

    /// <summary>When ledger already has entry: <c>RunAsync</c> skips candidate.</summary>
    [Fact]
    public async Task RunAsync_WhenLedgerAlreadyHasEntry_SkipsCandidate()
    {
        var entryId = Guid.NewGuid();
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", entryId, "Lunch", Now.AddMinutes(5))]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _ledger.ExistsAsync(Arg.Is<MealEntryId>(id => id.Value == entryId), MealReminderKind.Reminder,
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(Arg.Any<MealReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().AddAsync(Arg.Any<SentMealReminder>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Mixed candidates: <c>RunAsync</c> publishes only for unseen and commits once.</summary>
    [Fact]
    public async Task RunAsync_MixedCandidates_PublishesOnlyForUnseenAndCommitsOnce()
    {
        var dueId1 = Guid.NewGuid();
        var dueId2 = Guid.NewGuid();
        var missedId = Guid.NewGuid();

        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([
                new MealReminderCandidate("u1", "en", dueId1, "Lunch", Now.AddMinutes(10)),
                new MealReminderCandidate("u1", "en", dueId2, "Snack", Now.AddMinutes(20))
            ]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", missedId, "Breakfast", Now.AddHours(-2))]);

        // dueId1 already sent; dueId2 and missedId fresh
        _ledger.ExistsAsync(Arg.Is<MealEntryId>(id => id.Value == dueId1), MealReminderKind.Reminder, Arg.Any<CancellationToken>())
            .Returns(true);
        _ledger.ExistsAsync(Arg.Is<MealEntryId>(id => id.Value == dueId2), MealReminderKind.Reminder, Arg.Any<CancellationToken>())
            .Returns(false);
        _ledger.ExistsAsync(Arg.Is<MealEntryId>(id => id.Value == missedId), MealReminderKind.Missed, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunAsync(Now, CancellationToken.None);

        // dueId1 skipped
        await _bus.DidNotReceive().PublishAsync(
            Arg.Is<MealReminderDueIntegrationEvent>(e => e.MealEntryId == dueId1),
            Arg.Any<CancellationToken>());
        // dueId2 published
        await _bus.Received(1).PublishAsync(
            Arg.Is<MealReminderDueIntegrationEvent>(e => e.MealEntryId == dueId2),
            Arg.Any<CancellationToken>());
        // missedId published
        await _bus.Received(1).PublishAsync(
            Arg.Is<MealMissedIntegrationEvent>(e => e.MealEntryId == missedId),
            Arg.Any<CancellationToken>());

        await _ledger.Received(2).AddAsync(Arg.Any<SentMealReminder>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
