namespace Notifications.IntegrationTests.Api;

using System.Net.Http.Json;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;
using Notifications.IntegrationTests.Infrastructure;

/// <summary>HTTP inbox tests against the real notifications database.</summary>
[Collection(NotificationsDatabaseCollectionDefinition.Name)]
public sealed class NotificationsEndpointsTests(NotificationsPostgresFixture fixture) : IDisposable
{
    private readonly NotificationsApiFactory _factory = new(fixture.ConnectionString);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    /// <summary>Inbox renders persisted notification text in the requested language.</summary>
    [Fact]
    public async Task ListNotifications_PolishHeader_RendersPolishText()
    {
        using var client = _factory.CreateClientWithRoles();
        var userId = client.DefaultRequestHeaders.GetValues("X-Test-Sub").Single();
        await using (var uow = new DapperUnitOfWork(new NotificationsConnectionFactory(fixture.ConnectionString)))
        {
            var repo = new NotificationRepository(uow);
            await repo.AddAsync(Notification.Create(NotificationId.New(), userId,
                NotificationType.WaterReminder, "Water break", "Time to drink some water.",
                "{}", new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)),
                TestContext.Current.CancellationToken);
            await uow.CommitAsync(TestContext.Current.CancellationToken);
        }

        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pl");
        var response = await client.GetFromJsonAsync<Page>("/api/notifications",
            TestContext.Current.CancellationToken);

        response.ShouldNotBeNull();
        response.Items.ShouldHaveSingleItem().Title.ShouldBe("Przerwa na wodę");
        response.Items.Single().Body.ShouldBe("Czas na wypicie wody.");
    }

    private sealed record Page(IReadOnlyList<Item> Items);

    private sealed record Item(string Title, string Body);
}
