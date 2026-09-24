namespace Household.Domain.Aggregates;

using global::Household.Domain.Events;
using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A human known to the system. Either linked to an Authentik account
/// (<see cref="AuthSubject"/> set) or <em>managed</em> — created by an adult for someone
/// who has no login yet, e.g. a child. A managed person can later be linked to a real
/// account via <see cref="LinkAuthSubject"/> without losing any history, because personal
/// data keys on the person id, never on the Authentik subject.
/// </summary>
public sealed class Person : AggregateRoot<PersonId>
{
    private const int MaxDisplayNameLength = 200;

    private Person() { }

    /// <summary>Registers the person behind an OIDC login on their first sign-in.</summary>
    /// <param name="id">Identifier for the new person.</param>
    /// <param name="authSubject">The Authentik subject claim; required.</param>
    /// <param name="displayName">Name from the OIDC profile; falls back to the subject when blank.</param>
    /// <param name="email">Email from the OIDC profile, if present.</param>
    /// <param name="avatarUrl">Avatar from the OIDC profile, if present.</param>
    /// <exception cref="ArgumentException"><paramref name="authSubject"/> is blank.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static Person RegisterFromLogin(
        PersonId id,
        string authSubject,
        string displayName,
        PersonEmail? email,
        string? avatarUrl,
        DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authSubject);

        var person = new Person
        {
            Id = id,
            AuthSubject = authSubject,
            DisplayName = NormaliseDisplayName(displayName, authSubject),
            Email = email,
            AvatarUrl = NullIfBlank(avatarUrl),
            IsManaged = false,
            CreatedAt = now,
        };

        person.RaiseDomainEvent(new PersonRegisteredDomainEvent(id, IsManaged: false));
        return person;
    }

    /// <summary>Creates a managed person (no login) — e.g. a child an adult logs data for.</summary>
    /// <param name="id">Identifier for the new person.</param>
    /// <param name="displayName">Name shown across the app; required.</param>
    /// <param name="email">Optional address a future login can be matched against.</param>
    /// <exception cref="HouseholdDomainException"><paramref name="displayName"/> is blank.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static Person CreateManaged(PersonId id, string displayName, PersonEmail? email, DateTime now)
    {
        var person = new Person
        {
            Id = id,
            AuthSubject = null,
            DisplayName = NormaliseDisplayName(displayName, fallback: null),
            Email = email,
            AvatarUrl = null,
            IsManaged = true,
            CreatedAt = now,
        };

        person.RaiseDomainEvent(new PersonRegisteredDomainEvent(id, IsManaged: true));
        return person;
    }

    /// <summary>The Authentik subject this person signs in as; <see langword="null"/> for a managed person.</summary>
    public string? AuthSubject { get; private set; }
    /// <summary>Name shown across the app; never blank, at most 200 characters.</summary>
    public string DisplayName { get; private set; } = default!;
    /// <summary>The person's email, if known; for a managed person it is the address a future login is matched against.</summary>
    public PersonEmail? Email { get; private set; }
    /// <summary>Avatar URL from the OIDC profile, if any.</summary>
    public string? AvatarUrl { get; private set; }
    /// <summary>True while the person has no login of their own and is maintained by an adult.</summary>
    public bool IsManaged { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last profile or link change, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Whether an Authentik account is attached.</summary>
    public bool IsLinked => AuthSubject is not null;

    /// <summary>Refreshes profile fields from the latest OIDC claims. No-op when nothing changed.</summary>
    /// <param name="displayName">Name from the OIDC profile; falls back to the subject when blank.</param>
    /// <param name="email">Email from the OIDC profile, if present.</param>
    /// <param name="avatarUrl">Avatar from the OIDC profile, if present.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public void RefreshProfile(string displayName, PersonEmail? email, string? avatarUrl, DateTime now)
    {
        var newName = NormaliseDisplayName(displayName, AuthSubject);
        var newAvatar = NullIfBlank(avatarUrl);

        if (newName == DisplayName
            && Equals(email, Email)
            && newAvatar == AvatarUrl)
        {
            return;
        }

        DisplayName = newName;
        Email = email;
        AvatarUrl = newAvatar;
        UpdatedAt = now;
    }

    /// <summary>
    /// Records the email a managed person's future login will be matched against, so an
    /// adult can hand a child (or anyone) a real account without losing history. The link
    /// completes on that person's first sign-in — see <see cref="LinkAuthSubject"/>.
    /// </summary>
    /// <param name="email">The address the future login must present.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="ArgumentNullException"><paramref name="email"/> is null.</exception>
    /// <exception cref="HouseholdDomainException">The person is not managed, or is already linked.</exception>
    public void MarkPendingAccountLink(PersonEmail email, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (!IsManaged)
            throw new HouseholdDomainException("Only a managed person can be converted to a real account.");

        if (IsLinked)
            throw new HouseholdDomainException("This person is already linked to an account.");

        Email = email;
        UpdatedAt = now;
    }

    /// <summary>Links a managed person to the Authentik account that just signed in as them and raises <see cref="PersonLinkedToAccountDomainEvent"/>.</summary>
    /// <param name="authSubject">The Authentik subject claim; required.</param>
    /// <exception cref="ArgumentException"><paramref name="authSubject"/> is blank.</exception>
    /// <exception cref="HouseholdDomainException">The person is already linked.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public void LinkAuthSubject(string authSubject, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authSubject);

        if (IsLinked)
            throw new HouseholdDomainException("This person is already linked to an account.");

        AuthSubject = authSubject;
        IsManaged = false;
        UpdatedAt = now;
        RaiseDomainEvent(new PersonLinkedToAccountDomainEvent(Id, authSubject));
    }

    private static string NormaliseDisplayName(string displayName, string? fallback)
    {
        var candidate = displayName?.Trim();
        if (string.IsNullOrEmpty(candidate))
        {
            candidate = fallback?.Trim();
            if (string.IsNullOrEmpty(candidate))
                throw new HouseholdDomainException("A person must have a display name.");
        }

        return candidate.Length > MaxDisplayNameLength
            ? candidate[..MaxDisplayNameLength]
            : candidate;
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
