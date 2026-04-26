namespace Shared.Messaging.IntegrationTests.Dapper;

using global::Dapper;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Dapper.Inbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollection))]
public sealed class DapperInboxExecutorIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    public DapperInboxExecutorIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

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
    public async Task ExecuteAsync_FirstCall_InvokesHandlerAndInsertsRow()
    {
        var factory = new TestNpgsqlConnectionFactory(_fixture.ConnectionString);
        var sut = new DapperInboxExecutor<TestNpgsqlConnectionFactory>(factory);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
        await using var conn = await factory.OpenAsync();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM inbox_messages WHERE event_id = @EventId", new { EventId = eventId });
        count.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_SecondCallSameEventId_SkipsHandler()
    {
        var factory = new TestNpgsqlConnectionFactory(_fixture.ConnectionString);
        var sut = new DapperInboxExecutor<TestNpgsqlConnectionFactory>(factory);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);
        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
    }
}
