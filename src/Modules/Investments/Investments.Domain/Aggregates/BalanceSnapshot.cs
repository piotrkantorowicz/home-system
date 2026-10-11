namespace Investments.Domain.Aggregates;

using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A wallet's total value on one date, in the wallet's currency. One per wallet and date: recording
/// the same date again corrects it. A separate aggregate so a wallet never loads its history.
/// </summary>
public sealed class BalanceSnapshot : AggregateRoot<BalanceSnapshotId>
{
    /// <summary>Maximum length of a note.</summary>
    public const int MaxNoteLength = 200;

    private BalanceSnapshot() { }

    /// <summary>Records a balance for a date that has none yet.</summary>
    /// <param name="id">Identifier for the new snapshot.</param>
    /// <param name="wallet">The wallet; must not be archived.</param>
    /// <param name="asOf">The date the balance applies to; not after <paramref name="today"/>.</param>
    /// <param name="amount">The balance in the wallet's currency.</param>
    /// <param name="note">Optional note, trimmed, at most 200 characters.</param>
    /// <param name="recordedByPersonId">Who recorded it.</param>
    /// <param name="recordedByName">Their display name at the time (kept as a snapshot).</param>
    /// <param name="today">The household's current date.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="InvestmentsDomainException">The wallet is archived, the date is in the future, or the note is too long.</exception>
    public static BalanceSnapshot Record(
        BalanceSnapshotId id,
        Wallet wallet,
        DateOnly asOf,
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
            throw new InvestmentsDomainException("An archived wallet takes no new balances. Restore it first.");
        EnsureNotFuture(asOf, today);

        return new BalanceSnapshot
        {
            Id = id,
            WalletId = wallet.Id,
            HouseholdId = wallet.HouseholdId,
            AsOf = asOf,
            Amount = amount.Amount,
            Note = NormalizeNote(note),
            RecordedByPersonId = recordedByPersonId,
            RecordedByName = recordedByName.Length > Wallet.MaxOwnerNameLength ? recordedByName[..Wallet.MaxOwnerNameLength] : recordedByName,
            RecordedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Replaces the amount and note. Allowed on archived wallets too.</summary>
    /// <param name="amount">The corrected balance.</param>
    /// <param name="note">The corrected note.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="InvestmentsDomainException">The note is too long.</exception>
    public void Correct(Money amount, string? note, DateTimeOffset now)
    {
        var normalized = NormalizeNote(note);
        if (Amount == amount.Amount && Note == normalized)
            return;

        Amount = amount.Amount;
        Note = normalized;
        UpdatedAt = now;
    }

    private static void EnsureNotFuture(DateOnly asOf, DateOnly today)
    {
        if (asOf > today)
            throw new InvestmentsDomainException("A balance can't be recorded for a future date.");
    }

    private static string? NormalizeNote(string? note)
    {
        var trimmed = note?.Trim();
        if (trimmed?.Length > MaxNoteLength)
            throw new InvestmentsDomainException($"A note must be at most {MaxNoteLength} characters.");

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>The wallet this balance belongs to.</summary>
    public WalletId WalletId { get; private set; } = default!;

    /// <summary>The wallet's household (denormalised; a composite FK keeps it consistent).</summary>
    public Guid HouseholdId { get; private set; }

    /// <summary>The date the balance applies to.</summary>
    public DateOnly AsOf { get; private set; }

    /// <summary>The balance in the wallet's currency, two decimals.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Optional note.</summary>
    public string? Note { get; private set; }

    /// <summary>Who first recorded the balance for this date.</summary>
    public Guid RecordedByPersonId { get; private set; }

    /// <summary>Their display name when they recorded it.</summary>
    public string RecordedByName { get; private set; } = default!;

    /// <summary>When it was first recorded, UTC.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>When the amount or note last changed, UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }
}
