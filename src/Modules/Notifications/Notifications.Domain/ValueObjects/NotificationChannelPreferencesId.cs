namespace Notifications.Domain.ValueObjects;

/// <summary>
/// Typed identifier of a user's <c>NotificationChannelPreferences</c> row. Wrapping the <see cref="Guid"/> stops
/// identifiers of different models being swapped at compile time; equality is by value.
/// </summary>
/// <param name="Value">The underlying database key.</param>
public sealed record NotificationChannelPreferencesId(Guid Value)
{
    /// <summary>Generates a new, unique identifier for a row being created.</summary>
    public static NotificationChannelPreferencesId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing key read from storage or a request.</summary>
    /// <param name="value">The raw key.</param>
    public static NotificationChannelPreferencesId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
