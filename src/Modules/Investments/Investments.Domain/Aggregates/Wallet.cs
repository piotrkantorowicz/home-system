namespace Investments.Domain.Aggregates;

using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One account at one institution, in one person's name. Owner, kind and currency are fixed at
/// creation; name and visibility can change. Balances live in a separate aggregate.
/// </summary>
public sealed class Wallet : AggregateRoot<WalletId>
{
    /// <summary>Maximum length of a wallet name.</summary>
    public const int MaxNameLength = 80;

    /// <summary>Maximum length of the stored owner name snapshot.</summary>
    public const int MaxOwnerNameLength = 200;

    private Wallet() { }

    /// <summary>Creates a wallet.</summary>
    /// <param name="id">Identifier for the new wallet.</param>
    /// <param name="householdId">The household it belongs to.</param>
    /// <param name="ownerPersonId">The legal owner.</param>
    /// <param name="ownerName">The owner's display name at creation (kept as a snapshot).</param>
    /// <param name="name">Display name; trimmed, 1–80 characters.</param>
    /// <param name="institution">Where the account is held.</param>
    /// <param name="kind">What kind of account it is.</param>
    /// <param name="currency">The currency balances are recorded in.</param>
    /// <param name="visibility">Who can see it.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="InvestmentsDomainException">The name is blank or too long.</exception>
    public static Wallet Create(
        WalletId id,
        Guid householdId,
        Guid ownerPersonId,
        string ownerName,
        string name,
        WalletInstitution institution,
        WalletKind kind,
        WalletCurrency currency,
        WalletVisibility visibility,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        return new Wallet
        {
            Id = id,
            HouseholdId = householdId,
            OwnerPersonId = ownerPersonId,
            OwnerName = ownerName.Length > MaxOwnerNameLength ? ownerName[..MaxOwnerNameLength] : ownerName,
            Name = NormalizeName(name),
            Institution = institution,
            Kind = kind,
            Currency = currency,
            Visibility = visibility,
            CreatedAt = now,
        };
    }

    /// <summary>Renames the wallet.</summary>
    /// <param name="name">New display name; trimmed, 1–80 characters.</param>
    /// <exception cref="InvestmentsDomainException">The name is blank or too long.</exception>
    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Changes who can see the wallet. History is not rewritten.</summary>
    /// <param name="visibility">The new visibility.</param>
    public void ChangeVisibility(WalletVisibility visibility) => Visibility = visibility;

    /// <summary>Hides the wallet from active lists and totals; history stays. No-op when already archived.</summary>
    public void Archive() => IsArchived = true;

    /// <summary>Makes an archived wallet active again. No-op when not archived.</summary>
    public void Restore() => IsArchived = false;

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength)
            throw new InvestmentsDomainException($"A wallet name must be 1–{MaxNameLength} characters.");

        return trimmed;
    }

    /// <summary>The household this wallet belongs to.</summary>
    public Guid HouseholdId { get; private set; }

    /// <summary>The legal owner.</summary>
    public Guid OwnerPersonId { get; private set; }

    /// <summary>The owner's display name when the wallet was created; shown once they leave the roster.</summary>
    public string OwnerName { get; private set; } = default!;

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Where the account is held.</summary>
    public WalletInstitution Institution { get; private set; }

    /// <summary>What kind of account it is.</summary>
    public WalletKind Kind { get; private set; }

    /// <summary>The currency balances are recorded in.</summary>
    public WalletCurrency Currency { get; private set; }

    /// <summary>Who can see the wallet.</summary>
    public WalletVisibility Visibility { get; private set; }

    /// <summary>Whether the wallet is hidden from active lists and totals.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Creation time, UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Concurrency token mapped to PostgreSQL's <c>xmin</c> (see <c>WalletConfiguration</c>); never set
    /// by domain logic. Clients send it back on edit so a stale edit is a conflict.
    /// </summary>
    public uint Version { get; private set; }
}
