namespace Household.Domain.Aggregates;

using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A human known to the system. Either linked to an Authentik account
/// (<see cref="AuthSubject"/> set) or <em>managed</em> — created by an adult for someone
/// who has no login yet, e.g. a child. A managed person can later be linked to a real
/// account via <see cref="LinkAuthSubject"/> without losing any history, because personal
/// data keys on <see cref="Id"/>, never on the Authentik subject.
/// </summary>
public sealed class Person : AggregateRoot<PersonId>
{
    private const int MaxDisplayNameLength = 200;

    private Person() { }

    /// <summary>Registers the person behind an OIDC login on their first sign-in.</summary>
    public static Person RegisterFromLogin(
        PersonId id,
        string authSubject,
        string displayName,
        PersonEmail? email,
        string? avatarUrl)
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
            CreatedAt = DateTime.UtcNow,
        };

        person.RaiseDomainEvent(new PersonRegisteredDomainEvent(id, IsManaged: false));
        return person;
    }

    /// <summary>Creates a managed person (no login) — e.g. a child an adult logs data for.</summary>
    public static Person CreateManaged(PersonId id, string displayName, PersonEmail? email)
    {
        var person = new Person
        {
            Id = id,
            AuthSubject = null,
            DisplayName = NormaliseDisplayName(displayName, fallback: null),
            Email = email,
            AvatarUrl = null,
            IsManaged = true,
            CreatedAt = DateTime.UtcNow,
        };

        person.RaiseDomainEvent(new PersonRegisteredDomainEvent(id, IsManaged: true));
        return person;
    }

    public string? AuthSubject { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public PersonEmail? Email { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsManaged { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsLinked => AuthSubject is not null;

    /// <summary>Refreshes profile fields from the latest OIDC claims. No-op when nothing changed.</summary>
    public void RefreshProfile(string displayName, PersonEmail? email, string? avatarUrl)
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
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Links a managed person to the Authentik account that just signed in as them.</summary>
    public void LinkAuthSubject(string authSubject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authSubject);

        if (IsLinked)
            throw new HouseholdDomainException("This person is already linked to an account.");

        AuthSubject = authSubject;
        IsManaged = false;
        UpdatedAt = DateTime.UtcNow;
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
