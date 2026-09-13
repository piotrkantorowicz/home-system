namespace Shared.Messaging.IntegrationTests.Ef;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollectionDefinition))]
public sealed class EfInboxExecutorIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    public EfInboxExecutorIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

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
    public async Task ExecuteAsync_FirstCall_InvokesHandlerAndPersistsInboxRow()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
        var rowExists = await _dbContext.Set<InboxMessageEntity>().AnyAsync(x => x.EventId == eventId);
        rowExists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SecondCallSameEventId_SkipsHandler()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);
        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_HandlerThrows_DoesNotPersistInboxRow()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext);
        var eventId = Guid.NewGuid();

        var act = () => sut.ExecuteAsync(
            eventId, "X.Y",
            _ => throw new InvalidOperationException("boom"),
            default);

        await act.ShouldThrowAsync<InvalidOperationException>();

        var rowExists = await _dbContext.Set<InboxMessageEntity>().AnyAsync(x => x.EventId == eventId);
        rowExists.ShouldBeFalse();
    }
}
