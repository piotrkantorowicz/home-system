namespace Shared.Messaging.IntegrationTests.Ef;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

/// <summary>Integration tests for <c>EfOutboxStore</c> against a real PostgreSQL container.</summary>
[Collection(nameof(PostgresCollectionDefinition))]
public sealed class EfOutboxStoreIntegrationTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);

    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public EfOutboxStoreIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    /// <summary>Recreates the database schema through an EF <c>DbContext</c> that creates the messaging tables, so every test starts from empty tables.</summary>
    public async ValueTask InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _dbContext = new MessagingTestDbContext(options);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
    }


    /// <summary>Disposes the test <c>DbContext</c>.</summary>
    public ValueTask DisposeAsync() => _dbContext.DisposeAsync();

    /// <summary><c>AddAsync</c> persists row.</summary>
    [Fact]
    public async Task AddAsync_PersistsRow()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", """{"a":1}""",
            Now, null, 0, null);

        await sut.AddAsync(msg, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var unprocessed = await sut.GetUnprocessedAsync(10, 10, Now, TestContext.Current.CancellationToken);
        unprocessed.Count.ShouldBe(1);
        unprocessed[0].EventId.ShouldBe(msg.EventId);
    }

    /// <summary><c>MarkProcessedAsync</c> removes from unprocessed.</summary>
    [Fact]
    public async Task MarkProcessedAsync_RemovesFromUnprocessed()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}",
            Now, null, 0, null);

        await sut.AddAsync(msg, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await sut.MarkProcessedAsync(msg.Id, Now, TestContext.Current.CancellationToken);

        var unprocessed = await sut.GetUnprocessedAsync(10, 10, Now, TestContext.Current.CancellationToken);
        unprocessed.ShouldBeEmpty();
    }

    /// <summary><c>RecordFailureAsync</c> increments attempt count and stores error.</summary>
    [Fact]
    public async Task RecordFailureAsync_IncrementsAttemptCountAndStoresError()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}",
            Now, null, 0, null);

        await sut.AddAsync(msg, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await sut.RecordFailureAsync(msg.Id, "boom", Now, TestContext.Current.CancellationToken);
        await sut.RecordFailureAsync(msg.Id, "boom2", Now, TestContext.Current.CancellationToken);

        var unprocessed = await sut.GetUnprocessedAsync(10, 10, Now, TestContext.Current.CancellationToken);
        unprocessed[0].AttemptCount.ShouldBe(2);
        unprocessed[0].LastError.ShouldBe("boom2");
    }

    /// <summary>A failed message is skipped until its scheduled retry time, then picked up again.</summary>
    [Fact]
    public async Task RecordFailureAsync_SkipsMessageUntilNextAttemptAt()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now, null, 0, null);
        await sut.AddAsync(msg, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await sut.RecordFailureAsync(msg.Id, "boom", Now.AddSeconds(30), TestContext.Current.CancellationToken);

        (await sut.GetUnprocessedAsync(10, 10, Now.AddSeconds(29), TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await sut.GetUnprocessedAsync(10, 10, Now.AddSeconds(30), TestContext.Current.CancellationToken))
            .Single().Id.ShouldBe(msg.Id);
    }

    /// <summary>A message at the attempt limit is dead-lettered: skipped by the worker query, listed and counted as dead.</summary>
    [Fact]
    public async Task MessageAtAttemptLimit_IsSkipped_ListedAndCounted()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var dead = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now, null, 3, "boom");
        var retrying = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now.AddMinutes(1), null, 1, "boom");
        var fresh = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now.AddMinutes(2), null, 0, null);
        await sut.AddAsync(dead, TestContext.Current.CancellationToken);
        await sut.AddAsync(retrying, TestContext.Current.CancellationToken);
        await sut.AddAsync(fresh, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pending = await sut.GetUnprocessedAsync(10, 3, Now, TestContext.Current.CancellationToken);
        var list = await sut.ListAsync(3, 1, 10, TestContext.Current.CancellationToken);
        var backlog = await sut.CountAsync(3, TestContext.Current.CancellationToken);

        pending.Select(m => m.Id).ShouldBe([retrying.Id, fresh.Id]);
        list.TotalCount.ShouldBe(1);
        list.Items.Single().Id.ShouldBe(dead.Id);
        list.Items.Single().LastError.ShouldBe("boom");
        backlog.ShouldBe(new OutboxBacklog(DeadLettered: 1, Retrying: 1));
    }

    /// <summary>
    /// <c>RetryAsync</c> adds a fresh row for the same event that the worker picks up right away, and
    /// keeps the original (attempts, error) as history outside the dead-letter list and counters.
    /// </summary>
    [Fact]
    public async Task RetryAsync_AddsLinkedRow_AndKeepsOriginalAsHistory()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var dead = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", """{"a":1}""", Now, null, 3, "boom");
        await sut.AddAsync(dead, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await sut.RecordFailureAsync(dead.Id, "boom", Now.AddHours(1), TestContext.Current.CancellationToken);

        var retried = await sut.RetryAsync(dead.Id, Now, TestContext.Current.CancellationToken);

        retried.ShouldBeTrue();
        var pending = (await sut.GetUnprocessedAsync(10, 3, Now, TestContext.Current.CancellationToken)).Single();
        pending.Id.ShouldNotBe(dead.Id);
        pending.EventId.ShouldBe(dead.EventId);
        pending.Payload.ShouldBe("""{"a": 1}"""); // jsonb normalises whitespace
        pending.AttemptCount.ShouldBe(0);

        _dbContext.ChangeTracker.Clear();
        var original = await _dbContext.Set<OutboxMessageEntity>()
            .SingleAsync(x => x.Id == dead.Id, TestContext.Current.CancellationToken);
        original.AttemptCount.ShouldBe(4);
        original.LastError.ShouldBe("boom");
        original.RetriedAt.ShouldBe(Now);
        var replacement = await _dbContext.Set<OutboxMessageEntity>()
            .SingleAsync(x => x.Id == pending.Id, TestContext.Current.CancellationToken);
        replacement.RetryOf.ShouldBe(dead.Id);

        (await sut.ListAsync(3, 1, 10, TestContext.Current.CancellationToken)).TotalCount.ShouldBe(0);
        (await sut.CountAsync(3, TestContext.Current.CancellationToken)).ShouldBe(new OutboxBacklog(0, 0));
    }

    /// <summary>A retry that dies again is listed with a link to the row it retried.</summary>
    [Fact]
    public async Task RetryAsync_ReplacementThatDiesAgain_IsListedWithRetryOf()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var dead = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now, null, 1, "boom");
        await sut.AddAsync(dead, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await sut.RetryAsync(dead.Id, Now, TestContext.Current.CancellationToken);
        var replacement = (await sut.GetUnprocessedAsync(10, 1, Now, TestContext.Current.CancellationToken)).Single();

        await sut.RecordFailureAsync(replacement.Id, "boom again", Now, TestContext.Current.CancellationToken);

        var listed = (await sut.ListAsync(1, 1, 10, TestContext.Current.CancellationToken)).Items.Single();
        listed.Id.ShouldBe(replacement.Id);
        listed.RetryOf.ShouldBe(dead.Id);
    }

    /// <summary><c>RetryAsync</c> leaves processed, already retried and unknown messages alone and reports it.</summary>
    [Fact]
    public async Task RetryAsync_ProcessedRetriedOrUnknown_ReturnsFalse()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var done = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now, Now, 3, "boom");
        var dead = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}", Now, null, 3, "boom");
        await sut.AddAsync(done, TestContext.Current.CancellationToken);
        await sut.AddAsync(dead, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await sut.RetryAsync(dead.Id, Now, TestContext.Current.CancellationToken);

        (await sut.RetryAsync(done.Id, Now, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await sut.RetryAsync(dead.Id, Now, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await sut.RetryAsync(Guid.NewGuid(), Now, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
