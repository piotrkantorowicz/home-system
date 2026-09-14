namespace Household.Domain.ValueObjects;

using Household.Domain.Exceptions;

/// <summary>
/// A person's email address. Normalised to lower-case and trimmed. Used to match a
/// pending invitation or a managed-member link to an OIDC login.
/// </summary>
public sealed record PersonEmail
{
    private PersonEmail(string value) => Value = value;

    /// <summary>The normalised address: trimmed and lower-cased.</summary>
    public string Value { get; }

    /// <summary>Normalises and validates an address. Deliberately permissive — Authentik is the source of truth.</summary>
    /// <param name="value">The raw address; required.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is blank.</exception>
    /// <exception cref="HouseholdDomainException">The value has no single <c>@</c> with text on both sides.</exception>
    public static PersonEmail Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalised = value.Trim().ToLowerInvariant();

        // Deliberately permissive — Authentik is the source of truth for real addresses.
        var at = normalised.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != normalised.LastIndexOf('@') || at == normalised.Length - 1)
            throw new HouseholdDomainException($"'{value}' is not a valid email address.");

        return new PersonEmail(normalised);
    }

    /// <summary>Like <see cref="Create"/> but maps a blank input to <see langword="null"/> instead of throwing.</summary>
    /// <param name="value">The raw address, or blank.</param>
    public static PersonEmail? CreateOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Create(value);

    /// <inheritdoc />
    public override string ToString() => Value;
}
