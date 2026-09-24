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

        var unprocessed = await sut.GetUnprocessedAsync(10, TestContext.Current.CancellationToken);
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

        var unprocessed = await sut.GetUnprocessedAsync(10, TestContext.Current.CancellationToken);
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

        await sut.RecordFailureAsync(msg.Id, "boom", TestContext.Current.CancellationToken);
        await sut.RecordFailureAsync(msg.Id, "boom2", TestContext.Current.CancellationToken);

        var unprocessed = await sut.GetUnprocessedAsync(10, TestContext.Current.CancellationToken);
        unprocessed[0].AttemptCount.ShouldBe(2);
        unprocessed[0].LastError.ShouldBe("boom2");
    }
}
