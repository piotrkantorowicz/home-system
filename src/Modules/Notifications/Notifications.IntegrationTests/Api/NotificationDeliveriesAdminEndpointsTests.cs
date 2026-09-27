namespace Notifications.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;
using Notifications.Infrastructure.Workers;
using Notifications.IntegrationTests.Infrastructure;

/// <summary>
/// HTTP integration tests for the notification delivery admin endpoints: request → dispatcher →
/// handler → PostgreSQL (Testcontainers) → response, including the admin-only policy.
/// </summary>
[Collection(NotificationsDatabaseCollectionDefinition.Name)]
public sealed class NotificationDeliveriesAdminEndpointsTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
    private static readonly int MaxAttempts = new RetryDeliveryWorkerOptions().MaxAttempts;

    private readonly NotificationsPostgresFixture _fixture;
    private readonly NotificationsApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public NotificationDeliveriesAdminEndpointsTests(NotificationsPostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new NotificationsApiFactory(fixture.ConnectionString);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    /// <summary>Every delivery admin route answers 403 to an authenticated non-admin.</summary>
    [Theory]
    [InlineData("GET", "/api/admin/notifications/deliveries/summary")]
    [InlineData("GET", "/api/admin/notifications/deliveries/dead-letters")]
    [InlineData("POST", "/api/admin/notifications/deliveries/0198f5a4-0000-7000-8000-000000000000/retry")]
    public async Task NonAdmin_IsForbidden(string method, string url)
    {
        var client = _factory.CreateClientWithRoles();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A dead-lettered delivery is counted and listed; retry takes it off the list.</summary>
    [Fact]
    public async Task Admin_SeesDeadLetter_AndRetryRequeuesIt()
    {
        var deliveryId = await SeedFailedAsync(MaxAttempts);
        var client = _factory.CreateClientWithRoles("admin");

        var summary = await client.GetFromJsonAsync<Backlog>("/api/admin/notifications/deliveries/summary", TestContext.Current.CancellationToken);
        summary!.DeadLettered.ShouldBeGreaterThanOrEqualTo(1);

        var list = await client.GetFromJsonAsync<Page>("/api/admin/notifications/deliveries/dead-letters?pageSize=100", TestContext.Current.CancellationToken);
        var row = list!.Items.Single(i => i.DeliveryId == deliveryId);
        row.Channel.ShouldBe("Console");
        row.AttemptCount.ShouldBe(MaxAttempts);
        row.FailureReason.ShouldBe("smtp down");

        var retry = await client.PostAsync($"/api/admin/notifications/deliveries/{deliveryId}/retry", null, TestContext.Current.CancellationToken);
        retry.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await client.GetFromJsonAsync<Page>("/api/admin/notifications/deliveries/dead-letters?pageSize=100", TestContext.Current.CancellationToken);
        after!.Items.ShouldNotContain(i => i.DeliveryId == deliveryId);
    }

    /// <summary>Retry of a delivery that is not failed is a 422; of an unknown one a 404.</summary>
    [Fact]
    public async Task Admin_Retry_RejectsNonFailedAndUnknown()
    {
        var sentId = await SeedAsync(d => d.MarkSent(Now));
        var client = _factory.CreateClientWithRoles("admin");

        var notFailed = await client.PostAsync($"/api/admin/notifications/deliveries/{sentId}/retry", null, TestContext.Current.CancellationToken);
        var unknown = await client.PostAsync($"/api/admin/notifications/deliveries/{Guid.CreateVersion7()}/retry", null, TestContext.Current.CancellationToken);

        notFailed.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<Guid> SeedFailedAsync(int attempts)
        => SeedAsync(d =>
        {
            for (var i = 0; i < attempts; i++)
                d.MarkFailed(Now, "smtp down");
        });

    private async Task<Guid> SeedAsync(Action<NotificationDelivery> arrange)
    {
        await using var uow = new DapperUnitOfWork(new NotificationsConnectionFactory(_fixture.ConnectionString));
        var repo = new NotificationRepository(uow);

        var notificationId = NotificationId.New();
        await repo.AddAsync(Notification.Create(
            notificationId, $"user-{Guid.NewGuid():N}", NotificationType.WaterReminder,
            "Water", "Drink now", "{}", Now), TestContext.Current.CancellationToken);

        var delivery = NotificationDelivery.Create(NotificationDeliveryId.New(), notificationId, NotificationChannel.Console);
        arrange(delivery);
        await repo.AddDeliveryAsync(delivery, TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);
        return delivery.Id.Value;
    }

    private sealed record Backlog(int DeadLettered, int Retrying);

    private sealed record Page(IReadOnlyList<Item> Items, int TotalCount);

    private sealed record Item(Guid DeliveryId, string Channel, int AttemptCount, string? FailureReason);
}
