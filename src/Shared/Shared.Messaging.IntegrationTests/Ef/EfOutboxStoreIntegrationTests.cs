namespace Shared.Messaging.IntegrationTests.Ef;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollectionDefinition))]
public sealed class EfOutboxStoreIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    public EfOutboxStoreIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _dbContext = new MessagingTestDbContext(options);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Fact]
    public async Task AddAsync_PersistsRow()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", """{"a":1}""",
            DateTime.UtcNow, null, 0, null);

        await sut.AddAsync(msg, default);
        await _dbContext.SaveChangesAsync();

        var unprocessed = await sut.GetUnprocessedAsync(10, default);
        unprocessed.Count.ShouldBe(1);
        unprocessed[0].EventId.ShouldBe(msg.EventId);
    }

    [Fact]
    public async Task MarkProcessedAsync_RemovesFromUnprocessed()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}",
            DateTime.UtcNow, null, 0, null);

        await sut.AddAsync(msg, default);
        await _dbContext.SaveChangesAsync();
        await sut.MarkProcessedAsync(msg.Id, DateTime.UtcNow, default);

        var unprocessed = await sut.GetUnprocessedAsync(10, default);
        unprocessed.ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordFailureAsync_IncrementsAttemptCountAndStoresError()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}",
            DateTime.UtcNow, null, 0, null);

        await sut.AddAsync(msg, default);
        await _dbContext.SaveChangesAsync();

        await sut.RecordFailureAsync(msg.Id, "boom", default);
        await sut.RecordFailureAsync(msg.Id, "boom2", default);

        var unprocessed = await sut.GetUnprocessedAsync(10, default);
        unprocessed[0].AttemptCount.ShouldBe(2);
        unprocessed[0].LastError.ShouldBe("boom2");
    }
}
