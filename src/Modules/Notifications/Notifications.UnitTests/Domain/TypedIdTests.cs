namespace Notifications.UnitTests.Domain;

using System.Buffers.Binary;
using Notifications.Domain.ValueObjects;

/// <summary>
/// Unit tests for the module's typed ids: <c>New()</c> must yield version-7 GUIDs whose
/// leading 48-bit timestamp makes later values sort after earlier ones (see
/// <c>backend-ddd-patterns.md</c> § Typed IDs).
/// </summary>
public sealed class TypedIdTests
{
    /// <summary>One factory per typed id in the module, keyed by the id type name.</summary>
    public static TheoryData<string, Func<Guid>> Factories => new()
    {
        { nameof(NotificationChannelPreferencesId), () => NotificationChannelPreferencesId.New().Value },
        { nameof(NotificationDeliveryId), () => NotificationDeliveryId.New().Value },
        { nameof(NotificationId), () => NotificationId.New().Value },
    };

    /// <summary>Called twice: <c>New</c> returns version-7 values ordered by creation time.</summary>
    [Theory]
    [MemberData(nameof(Factories))]
    public void New_CalledTwice_ReturnsTimeOrderedVersion7Values(string idType, Func<Guid> factory)
    {
        var first = factory();
        var second = NewInLaterMillisecond(factory, first);

        first.Version.ShouldBe(7, idType);
        second.Version.ShouldBe(7, idType);
        second.ShouldNotBe(first, idType);
        second.CompareTo(first).ShouldBeGreaterThan(0, idType);
    }

    // Version-7 GUIDs are only ordered across milliseconds — the bits after the timestamp are
    // random — so keep generating until the millisecond has advanced past the first value.
    private static Guid NewInLaterMillisecond(Func<Guid> factory, Guid earlier)
    {
        long earlierTimestamp = UnixMilliseconds(earlier);
        Guid later;
        do
        {
            later = factory();
        }
        while (UnixMilliseconds(later) <= earlierTimestamp);

        return later;
    }

    private static long UnixMilliseconds(Guid guid)
        => BinaryPrimitives.ReadInt64BigEndian(guid.ToByteArray(bigEndian: true)) >>> 16;
}
