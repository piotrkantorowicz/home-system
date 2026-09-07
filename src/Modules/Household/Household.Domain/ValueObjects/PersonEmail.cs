namespace Household.Domain.ValueObjects;

using Household.Domain.Exceptions;

/// <summary>
/// A person's email address. Normalised to lower-case and trimmed. Used to match a
/// pending invitation or a managed-member link to an OIDC login.
/// </summary>
public sealed record PersonEmail
{
    private PersonEmail(string value) => Value = value;

    public string Value { get; }

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

    public static PersonEmail? CreateOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Create(value);

    public override string ToString() => Value;
}
