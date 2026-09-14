namespace Notifications.Domain.ValueObjects;

/// <summary>
/// Typed identifier of a <c>NotificationDelivery</c> — one channel attempt for a notification. Wrapping the <see cref="Guid"/> stops
/// identifiers of different models being swapped at compile time; equality is by value.
/// </summary>
/// <param name="Value">The underlying database key.</param>
public sealed record NotificationDeliveryId(Guid Value)
{
    /// <summary>Generates a new, unique identifier for a row being created.</summary>
    public static NotificationDeliveryId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing key read from storage or a request.</summary>
    /// <param name="value">The raw key.</param>
    public static NotificationDeliveryId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
