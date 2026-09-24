namespace Notifications.IntegrationTests.Persistence;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;
using Notifications.IntegrationTests.Infrastructure;

/// <summary>Integration tests for <c>Repository</c> against a real PostgreSQL container.</summary>
[Collection(NotificationsDatabaseCollectionDefinition.Name)]
public sealed class RepositoryRoundtripTests
{
    private static readonly DateTime Now = new(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);

    private readonly NotificationsPostgresFixture _fixture;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public RepositoryRoundtripTests(NotificationsPostgresFixture fixture)
        => _fixture = fixture;

    private (NotificationsConnectionFactory Factory, DapperUnitOfWork Uow) CreateScope()
    {
        var factory = new NotificationsConnectionFactory(_fixture.ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        return (factory, uow);
    }

    /// <summary>Insert and read: <c>Notification</c> round trips.</summary>
    [Fact]
    public async Task Notification_InsertAndRead_RoundTrips()
    {
        var (_, uow) = CreateScope();
        await using var disposeUow = uow;
        var repo = new NotificationRepository(uow);
        var id = NotificationId.New();
        var notification = Notification.Create(
            id, $"user-{Guid.NewGuid():N}", NotificationType.MealReminder,
            "Lunch", "Eat now", """{"slot":"lunch"}""", Now);

        await repo.AddAsync(notification, TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        var (_, uow2) = CreateScope();
        await using var disposeUow2 = uow2;
        var roundtrip = await new NotificationRepository(uow2).GetByIdAsync(id, TestContext.Current.CancellationToken);

        roundtrip.ShouldNotBeNull();
        roundtrip.UserId.ShouldBe(notification.UserId);
        roundtrip.Type.ShouldBe(NotificationType.MealReminder);
        roundtrip.Title.ShouldBe("Lunch");
    }

    /// <summary>Insert and read: <c>NotificationDelivery</c> round trips.</summary>
    [Fact]
    public async Task NotificationDelivery_InsertAndRead_RoundTrips()
    {
        var (_, uow) = CreateScope();
        await using var disposeUow = uow;
        var repo = new NotificationRepository(uow);

        var notificationId = NotificationId.New();
        var notification = Notification.Create(
            notificationId, $"user-{Guid.NewGuid():N}", NotificationType.WaterReminder,
            "Water", "Drink now", "{}", Now);
        await repo.AddAsync(notification, TestContext.Current.CancellationToken);

        var deliveryId = NotificationDeliveryId.New();
        var delivery = NotificationDelivery.Create(deliveryId, notificationId, NotificationChannel.Console);
        await repo.AddDeliveryAsync(delivery, TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        var (_, uow2) = CreateScope();
        await using var disposeUow2 = uow2;
        var roundtrip = await new NotificationRepository(uow2).GetDeliveryAsync(deliveryId, TestContext.Current.CancellationToken);

        roundtrip.ShouldNotBeNull();
        roundtrip.Channel.ShouldBe(NotificationChannel.Console);
        roundtrip.Status.ShouldBe(DeliveryStatus.Pending);
        roundtrip.NotificationId.ShouldBe(notificationId);
    }

    /// <summary>Insert update read: <c>ChannelPreferences</c> round trips.</summary>
    [Fact]
    public async Task ChannelPreferences_InsertUpdateRead_RoundTrips()
    {
        var userId = $"user-{Guid.NewGuid():N}";
        var (_, uow) = CreateScope();
        await using var disposeUow = uow;
        var repo = new NotificationChannelPreferencesRepository(uow);

        var prefs = NotificationChannelPreferences.CreateDefault(
            NotificationChannelPreferencesId.New(), userId, Now);
        await repo.AddAsync(prefs, TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        var (_, uow2) = CreateScope();
        await using var disposeUow2 = uow2;
        var loaded = await new NotificationChannelPreferencesRepository(uow2)
            .GetByUserIdAsync(userId, TestContext.Current.CancellationToken);
        loaded.ShouldNotBeNull();
        loaded.ConsoleEnabled.ShouldBeTrue();
        loaded.EmailEnabled.ShouldBeTrue();

        loaded.Update(consoleEnabled: false, emailEnabled: true, webSocketEnabled: false, updatedAt: Now);

        var (_, uow3) = CreateScope();
        await using var disposeUow3 = uow3;
        var updateRepo = new NotificationChannelPreferencesRepository(uow3);
        await updateRepo.UpdateAsync(loaded, TestContext.Current.CancellationToken);
        await uow3.CommitAsync(TestContext.Current.CancellationToken);

        var (_, uow4) = CreateScope();
        await using var disposeUow4 = uow4;
        var refetched = await new NotificationChannelPreferencesRepository(uow4)
            .GetByUserIdAsync(userId, TestContext.Current.CancellationToken);
        refetched.ShouldNotBeNull();
        refetched.ConsoleEnabled.ShouldBeFalse();
        refetched.EmailEnabled.ShouldBeTrue();
        refetched.WebSocketEnabled.ShouldBeFalse();
    }

    /// <summary><c>InboxStore</c> records and detects duplicates.</summary>
    [Fact]
    public async Task InboxStore_RecordsAndDetectsDuplicates()
    {
        var (factory, _) = CreateScope();
        var store = new InboxStore(factory);
        var eventId = Guid.NewGuid();

        (await store.ExistsAsync(eventId, TestContext.Current.CancellationToken)).ShouldBeFalse();

        await store.RecordAsync(eventId, "Some.Event", Now, TestContext.Current.CancellationToken);

        (await store.ExistsAsync(eventId, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    /// <summary><c>DbUp</c> is idempotent.</summary>
    [Fact]
    public async Task DbUp_IsIdempotent()
    {
        var (_, _) = CreateScope();
        Notifications.Infrastructure.Persistence.Migrations.DbUpRunner.Run(_fixture.ConnectionString);
        Notifications.Infrastructure.Persistence.Migrations.DbUpRunner.Run(_fixture.ConnectionString);
    }
}
