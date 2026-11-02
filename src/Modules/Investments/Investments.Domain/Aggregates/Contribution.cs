namespace Investments.Domain.Aggregates;

using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Money put into or taken out of a wallet on one date, in the wallet's currency. Their net sum is
/// the capital invested, which the wallet's balance is compared against. Several per date are allowed.
/// </summary>
public sealed class Contribution : AggregateRoot<ContributionId>
{
    /// <summary>Maximum length of a note.</summary>
    public const int MaxNoteLength = BalanceSnapshot.MaxNoteLength;

    private Contribution() { }

    /// <summary>Records a deposit or withdrawal.</summary>
    /// <param name="id">Identifier for the new contribution.</param>
    /// <param name="wallet">The wallet; must not be archived.</param>
    /// <param name="date">When the money moved; from <see cref="BalanceSnapshot.EarliestAsOf"/> up to <paramref name="today"/>.</param>
    /// <param name="direction">Into or out of the wallet.</param>
    /// <param name="amount">The amount in the wallet's currency; greater than zero.</param>
    /// <param name="note">Optional note, trimmed, at most 200 characters.</param>
    /// <param name="recordedByPersonId">Who recorded it.</param>
    /// <param name="recordedByName">Their display name at the time (kept as a snapshot).</param>
    /// <param name="today">The household's current date.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="InvestmentsDomainException">The wallet is archived, the amount is zero, the date is out of range, or the note is too long.</exception>
    public static Contribution Record(
        ContributionId id,
        Wallet wallet,
        DateOnly date,
        ContributionDirection direction,
        Money amount,
        string? note,
        Guid recordedByPersonId,
        string recordedByName,
        DateOnly today,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedByName);

        if (wallet.IsArchived)
            throw new InvestmentsDomainException("An archived wallet takes no new deposits or withdrawals. Restore it first.");
        if (amount.MinorUnits == 0)
            throw new InvestmentsDomainException("A deposit or withdrawal must be greater than zero.");
        if (date > today)
            throw new InvestmentsDomainException("A deposit or withdrawal can't be recorded for a future date.");
        if (date < BalanceSnapshot.EarliestAsOf)
            throw new InvestmentsDomainException($"A deposit or withdrawal can't be recorded before {BalanceSnapshot.EarliestAsOf:yyyy-MM-dd}.");

        var trimmed = note?.Trim();
        if (trimmed?.Length > MaxNoteLength)
            throw new InvestmentsDomainException($"A note must be at most {MaxNoteLength} characters.");

        return new Contribution
        {
            Id = id,
            WalletId = wallet.Id,
            HouseholdId = wallet.HouseholdId,
            Date = date,
            Direction = direction,
            Amount = amount.Amount,
            Note = string.IsNullOrEmpty(trimmed) ? null : trimmed,
            RecordedByPersonId = recordedByPersonId,
            RecordedByName = recordedByName.Length > Wallet.MaxOwnerNameLength ? recordedByName[..Wallet.MaxOwnerNameLength] : recordedByName,
            RecordedAt = now,
        };
    }

    /// <summary>The wallet the money moved into or out of.</summary>
    public WalletId WalletId { get; private set; } = default!;

    /// <summary>The wallet's household (denormalised; a composite FK keeps it consistent).</summary>
    public Guid HouseholdId { get; private set; }

    /// <summary>When the money moved.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Into or out of the wallet.</summary>
    public ContributionDirection Direction { get; private set; }

    /// <summary>The amount in the wallet's currency, two decimals, always positive.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Optional note.</summary>
    public string? Note { get; private set; }

    /// <summary>Who recorded it.</summary>
    public Guid RecordedByPersonId { get; private set; }

    /// <summary>Their display name when they recorded it.</summary>
    public string RecordedByName { get; private set; } = default!;

    /// <summary>When it was recorded, UTC.</summary>
    public DateTimeOffset RecordedAt { get; private set; }
}
