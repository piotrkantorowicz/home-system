namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using global::Household.Infrastructure.Persistence;
using Household.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// HTTP integration tests for the outbox admin endpoints, driven through the Household module's
/// outbox: admin-only access, the dead-letter list and retry against PostgreSQL (Testcontainers).
/// </summary>
public sealed class OutboxAdminEndpointsTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly HouseholdApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this class.</param>
    public OutboxAdminEndpointsTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    /// <summary>Every outbox admin route answers 403 to an authenticated non-admin.</summary>
    [Theory]
    [InlineData("GET", "/api/admin/outbox/summary")]
    [InlineData("GET", "/api/admin/outbox/household/dead-letters")]
    [InlineData("POST", "/api/admin/outbox/household/dead-letters/0198f5a4-0000-7000-8000-000000000000/retry")]
    public async Task NonAdmin_IsForbidden(string method, string url)
    {
        var client = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}");

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>An anonymous caller gets 401.</summary>
    [Fact]
    public async Task Anonymous_IsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/admin/outbox/household/dead-letters", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A dead-lettered message is listed, retry resets it, and it leaves the list.</summary>
    [Fact]
    public async Task Admin_ListsDeadLetter_AndRetryRequeuesIt()
    {
        var deadId = await SeedAsync(attemptCount: new OutboxWorkerOptions().MaxAttempts);
        var retryingId = await SeedAsync(attemptCount: 1);
        var client = AdminClient();

        var list = await client.GetFromJsonAsync<DeadLetterPage>(
            "/api/admin/outbox/household/dead-letters?pageSize=100", TestContext.Current.CancellationToken);
        list!.Items.ShouldContain(i => i.Id == deadId);
        list.Items.ShouldNotContain(i => i.Id == retryingId);

        var retry = await client.PostAsync($"/api/admin/outbox/Household/dead-letters/{deadId}/retry", null, TestContext.Current.CancellationToken);
        retry.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await client.GetFromJsonAsync<DeadLetterPage>(
            "/api/admin/outbox/household/dead-letters?pageSize=100", TestContext.Current.CancellationToken);
        after!.Items.ShouldNotContain(i => i.Id == deadId);
        // The live outbox worker may already have retried (and failed) it once.
        (await AttemptCountAsync(deadId)).ShouldBeLessThan(new OutboxWorkerOptions().MaxAttempts);
    }

    /// <summary>Retry of an unknown message or an unknown module is 404.</summary>
    [Fact]
    public async Task Admin_Retry_UnknownMessageOrModule_IsNotFound()
    {
        var client = AdminClient();

        var unknownMessage = await client.PostAsync($"/api/admin/outbox/household/dead-letters/{Guid.CreateVersion7()}/retry", null, TestContext.Current.CancellationToken);
        var unknownModule = await client.PostAsync($"/api/admin/outbox/nope/dead-letters/{Guid.CreateVersion7()}/retry", null, TestContext.Current.CancellationToken);

        unknownMessage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unknownModule.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
        return client;
    }

    private async Task<Guid> SeedAsync(int attemptCount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseholdDbContext>();
        var id = Guid.CreateVersion7();
        db.Set<OutboxMessageEntity>().Add(new OutboxMessageEntity
        {
            Id = id,
            EventId = Guid.CreateVersion7(),
            // Unresolvable type: if the worker picks the row up it fails again instead of delivering.
            EventType = "Missing.Event, Missing",
            Payload = "{}",
            OccurredAt = _factory.Clock.GetUtcNow().UtcDateTime,
            AttemptCount = attemptCount,
            LastError = "boom",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<int> AttemptCountAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseholdDbContext>();
        var row = await db.Set<OutboxMessageEntity>().FindAsync([id], TestContext.Current.CancellationToken);
        return row!.AttemptCount;
    }

    private sealed record DeadLetterPage(IReadOnlyList<OutboxDeadLetter> Items, int TotalCount);
}
