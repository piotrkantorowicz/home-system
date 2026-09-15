namespace Shared.Messaging.IntegrationTests.Ef;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

/// <summary>Integration tests for <c>EfInboxExecutor</c> against a real PostgreSQL container.</summary>
[Collection(nameof(PostgresCollectionDefinition))]
public sealed class EfInboxExecutorIntegrationTests : IAsyncLifetime, IAsyncDisposable
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero));

    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public EfInboxExecutorIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    /// <summary>Recreates the database schema through an EF <c>DbContext</c> that creates the messaging tables, so every test starts from empty tables.</summary>
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _dbContext = new MessagingTestDbContext(options);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    /// <summary>Disposes the test <c>DbContext</c>.</summary>
    public ValueTask DisposeAsync() => _dbContext.DisposeAsync();

    /// <summary>First call: <c>ExecuteAsync</c> invokes handler and persists inbox row.</summary>
    [Fact]
    public async Task ExecuteAsync_FirstCall_InvokesHandlerAndPersistsInboxRow()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext, Clock);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
        var rowExists = await _dbContext.Set<InboxMessageEntity>().AnyAsync(x => x.EventId == eventId);
        rowExists.ShouldBeTrue();
    }

    /// <summary>Second call same event id: <c>ExecuteAsync</c> skips handler.</summary>
    [Fact]
    public async Task ExecuteAsync_SecondCallSameEventId_SkipsHandler()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext, Clock);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);
        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
    }

    /// <summary>Handler throws: <c>ExecuteAsync</c> does not persist inbox row.</summary>
    [Fact]
    public async Task ExecuteAsync_HandlerThrows_DoesNotPersistInboxRow()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext, Clock);
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
