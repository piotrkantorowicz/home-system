namespace Budget.Domain.Aggregates;

using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A recorded repayment: one person already paid another to reduce shared debt. It is a household
/// record — no money moves. Creation fields never change; only voiding (with a reason) does.
/// </summary>
public sealed class Settlement : AggregateRoot<SettlementId>
{
    /// <summary>Maximum length of a note.</summary>
    public const int MaxNoteLength = 200;

    /// <summary>Maximum length of a void reason.</summary>
    public const int MaxReasonLength = 200;

    private Settlement() { }

    /// <summary>Records a repayment.</summary>
    /// <param name="id">Identifier for the new repayment.</param>
    /// <param name="budgetId">The budget.</param>
    /// <param name="from">Who paid, with a name snapshot.</param>
    /// <param name="to">Who received, with a name snapshot.</param>
    /// <param name="amount">Positive amount actually paid; may exceed the current suggestion.</param>
    /// <param name="paidOn">The date the payment was made.</param>
    /// <param name="note">Optional note, trimmed; blank becomes <see langword="null"/>.</param>
    /// <param name="addedBy">The human recording it.</param>
    /// <param name="clientRequestId">Idempotency key of the submission.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="BudgetDomainException">Sender and recipient are the same, or the note is too long.</exception>
    public static Settlement Create(
        SettlementId id,
        BudgetId budgetId,
        PersonRef from,
        PersonRef to,
        Money amount,
        DateOnly paidOn,
        string? note,
        PersonRef addedBy,
        Guid clientRequestId,
        DateTime now)
    {
        if (from.PersonId == to.PersonId)
            throw new BudgetDomainException("A repayment needs two different people.");

        var trimmed = NormalizeNote(note);
        return new Settlement
        {
            Id = id,
            BudgetId = budgetId,
            FromPersonId = from.PersonId,
            FromDisplayName = from.DisplayName,
            ToPersonId = to.PersonId,
            ToDisplayName = to.DisplayName,
            Amount = amount.Amount,
            PaidOn = paidOn,
            Note = trimmed,
            AddedByPersonId = addedBy.PersonId,
            AddedByDisplayName = addedBy.DisplayName,
            ClientRequestId = clientRequestId,
            CreatedAt = now,
            Revision = 1,
        };
    }

    /// <summary>Whether a retry carrying these fields is the same logical creation request.</summary>
    /// <param name="from">Sender.</param>
    /// <param name="to">Recipient.</param>
    /// <param name="amount">Amount.</param>
    /// <param name="paidOn">Payment date.</param>
    /// <param name="note">Note.</param>
    public bool MatchesCreation(Guid from, Guid to, Money amount, DateOnly paidOn, string? note)
        => FromPersonId == from && ToPersonId == to && Amount == amount.Amount && PaidOn == paidOn
           && Note == NormalizeNote(note);

    /// <summary>Voids the repayment: it stops counting and the debt is restored. Financial fields stay as recorded.</summary>
    /// <param name="expectedRevision">The revision the caller saw.</param>
    /// <param name="reason">Short reason, 1–200 characters after trimming.</param>
    /// <param name="actor">Who is voiding it.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="ConflictException">The repayment changed since <paramref name="expectedRevision"/>.</exception>
    /// <exception cref="BudgetDomainException">The reason is invalid.</exception>
    public void Void(int expectedRevision, string reason, PersonRef actor, DateTime now)
    {
        if (Revision != expectedRevision)
            throw new ConflictException("This repayment was changed by someone else. Reload and try again.");

        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxReasonLength)
            throw new BudgetDomainException($"A reason of 1–{MaxReasonLength} characters is required.");

        IsVoided = true;
        VoidedAt = now;
        VoidReason = trimmed;
        VoidedByPersonId = actor.PersonId;
        VoidedByDisplayName = actor.DisplayName;
        Revision++;
    }

    private static string? NormalizeNote(string? note)
    {
        var trimmed = note?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Length > MaxNoteLength
            ? throw new BudgetDomainException($"A note can be at most {MaxNoteLength} characters.")
            : trimmed;
    }

    /// <summary>The budget.</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary>Who paid.</summary>
    public Guid FromPersonId { get; private set; }

    /// <summary>Sender name snapshot.</summary>
    public string FromDisplayName { get; private set; } = default!;

    /// <summary>Who received.</summary>
    public Guid ToPersonId { get; private set; }

    /// <summary>Recipient name snapshot.</summary>
    public string ToDisplayName { get; private set; } = default!;

    /// <summary>Amount actually paid, two decimals.</summary>
    public decimal Amount { get; private set; }

    /// <summary>The date the payment was made.</summary>
    public DateOnly PaidOn { get; private set; }

    /// <summary>Optional note.</summary>
    public string? Note { get; private set; }

    /// <summary>The human who recorded it (immutable).</summary>
    public Guid AddedByPersonId { get; private set; }

    /// <summary>Recorder name snapshot.</summary>
    public string AddedByDisplayName { get; private set; } = default!;

    /// <summary>Idempotency key of the creating submission.</summary>
    public Guid ClientRequestId { get; private set; }

    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>1 on creation, 2 once voided.</summary>
    public int Revision { get; private set; }

    /// <summary>Whether the repayment was voided; voided repayments no longer count.</summary>
    public bool IsVoided { get; private set; }

    /// <summary>When it was voided, UTC.</summary>
    public DateTime? VoidedAt { get; private set; }

    /// <summary>Why it was voided.</summary>
    public string? VoidReason { get; private set; }

    /// <summary>Who voided it.</summary>
    public Guid? VoidedByPersonId { get; private set; }

    /// <summary>Voider name snapshot.</summary>
    public string? VoidedByDisplayName { get; private set; }

    /// <summary>Concurrency token mapped to PostgreSQL's <c>xmin</c>; never set by domain logic.</summary>
    public uint Version { get; private set; }
}
