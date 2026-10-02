namespace Budget.Domain.Aggregates;

using global::Budget.Domain.Entities;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One recorded purchase: money, category, date, payer/recorder with name snapshots, and the exact
/// stored shares of a shared expense. Created with revision 1 in the same transaction.
/// </summary>
public sealed class Expense : AggregateRoot<ExpenseId>
{
    /// <summary>Maximum length of an update/void reason.</summary>
    public const int MaxReasonLength = 200;

    private readonly List<ExpenseShare> _shares = [];
    private readonly List<ExpenseRevision> _revisions = [];

    private Expense() { }

    /// <summary>Records an expense and its creation revision.</summary>
    /// <param name="id">Identifier for the new expense.</param>
    /// <param name="account">The envelope; must accept new entries (not archived).</param>
    /// <param name="amount">Positive amount.</param>
    /// <param name="category">Reporting category.</param>
    /// <param name="occurredOn">Purchase date.</param>
    /// <param name="funding">Funding source.</param>
    /// <param name="paidBy">Whose money paid; required for <see cref="FundingSource.Individual"/>, forbidden for household funds.</param>
    /// <param name="addedBy">The human recording it.</param>
    /// <param name="participants">Adults sharing the cost equally; empty for household funds and personal expenses.</param>
    /// <param name="clientRequestId">Idempotency key of the submission.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="BudgetDomainException">The envelope is archived or the funding/payer/participants combination is invalid.</exception>
    public static Expense Create(
        ExpenseId id,
        BudgetAccount account,
        Money amount,
        ExpenseCategory category,
        DateOnly occurredOn,
        FundingSource funding,
        PersonRef? paidBy,
        PersonRef addedBy,
        IReadOnlyCollection<PersonRef> participants,
        Guid clientRequestId,
        DateTime now)
    {
        if (account.IsArchived)
            throw new BudgetDomainException("This envelope is archived and cannot receive new expenses.");

        ValidateFunding(funding, paidBy, participants);

        var expense = new Expense
        {
            Id = id,
            BudgetId = account.BudgetId,
            BudgetAccountId = account.Id,
            Amount = amount.Amount,
            Category = category,
            OccurredOn = occurredOn,
            FundingSource = funding,
            PaidByPersonId = paidBy?.PersonId,
            PaidByDisplayName = paidBy?.DisplayName,
            AddedByPersonId = addedBy.PersonId,
            AddedByDisplayName = addedBy.DisplayName,
            CreatedAt = now,
            Revision = 1,
        };

        if (participants.Count > 0)
            foreach (var (person, share) in EqualSplit.Compute(amount, participants))
                expense._shares.Add(new ExpenseShare(id, person, share));

        var request = new ExpenseRequest(
            "Create", account.Id.Value, amount.ToString(), category, occurredOn, funding,
            paidBy?.PersonId, [.. participants.Select(p => p.PersonId).Order()]);
        expense._revisions.Add(new ExpenseRevision(id, account.BudgetId, 1, addedBy.PersonId, addedBy.DisplayName, clientRequestId, request, null, expense.ToSnapshot(), now));

        return expense;
    }

    /// <summary>
    /// Corrects the expense in place and appends a full revision. Envelope, recorder and creation
    /// time never change. Shares are kept exactly when amount and participants are unchanged,
    /// otherwise recomputed for the supplied participants.
    /// </summary>
    /// <param name="expectedRevision">The revision the caller saw.</param>
    /// <param name="reason">Short reason, 1–200 characters after trimming.</param>
    /// <param name="amount">New amount.</param>
    /// <param name="category">New category.</param>
    /// <param name="occurredOn">New purchase date.</param>
    /// <param name="funding">New funding source.</param>
    /// <param name="paidBy">New payer under the same rules as creation.</param>
    /// <param name="participants">New participants; may include people who left, but the caller only passes retained ones.</param>
    /// <param name="actor">Who is correcting it.</param>
    /// <param name="clientRequestId">Idempotency key of the submission.</param>
    /// <param name="request">The normalised request, stored with the revision.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="ConflictException">The expense changed since <paramref name="expectedRevision"/>.</exception>
    /// <exception cref="BudgetDomainException">The expense is void, the reason is invalid, or the funding combination is invalid.</exception>
    public void Update(
        int expectedRevision,
        string reason,
        Money amount,
        ExpenseCategory category,
        DateOnly occurredOn,
        FundingSource funding,
        PersonRef? paidBy,
        IReadOnlyCollection<PersonRef> participants,
        PersonRef actor,
        Guid clientRequestId,
        ExpenseRequest request,
        DateTime now)
    {
        EnsureEditable(expectedRevision);
        var trimmedReason = NormalizeReason(reason);
        ValidateFunding(funding, paidBy, participants);

        var sameShares = amount.Amount == Amount
            && participants.Select(p => p.PersonId).Order().SequenceEqual(_shares.Select(s => s.PersonId).Order());

        Amount = amount.Amount;
        Category = category;
        OccurredOn = occurredOn;
        FundingSource = funding;
        PaidByPersonId = paidBy?.PersonId;
        PaidByDisplayName = paidBy?.DisplayName;

        if (!sameShares)
            ReplaceShares(participants.Count == 0 ? [] : EqualSplit.Compute(amount, participants));

        AppendRevision(actor, clientRequestId, request, trimmedReason, now);
    }

    /// <summary>Marks the expense void and appends a revision. Financial fields stay as they were.</summary>
    /// <param name="expectedRevision">The revision the caller saw.</param>
    /// <param name="reason">Short reason, 1–200 characters after trimming.</param>
    /// <param name="actor">Who is voiding it.</param>
    /// <param name="clientRequestId">Idempotency key of the submission.</param>
    /// <param name="request">The normalised request, stored with the revision.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="ConflictException">The expense changed since <paramref name="expectedRevision"/>.</exception>
    /// <exception cref="BudgetDomainException">The expense is already void or the reason is invalid.</exception>
    public void Void(int expectedRevision, string reason, PersonRef actor, Guid clientRequestId, ExpenseRequest request, DateTime now)
    {
        EnsureEditable(expectedRevision);
        var trimmedReason = NormalizeReason(reason);

        IsVoided = true;
        VoidedAt = now;
        AppendRevision(actor, clientRequestId, request, trimmedReason, now);
    }

    private void EnsureEditable(int expectedRevision)
    {
        if (IsVoided)
            throw new BudgetDomainException("A voided expense cannot be changed.");

        if (Revision != expectedRevision)
            throw new ConflictException("This expense was changed by someone else. Reload and try again.");
    }

    private void AppendRevision(PersonRef actor, Guid clientRequestId, ExpenseRequest request, string reason, DateTime now)
    {
        Revision++;
        _revisions.Add(new ExpenseRevision(Id, BudgetId, Revision, actor.PersonId, actor.DisplayName, clientRequestId, request, reason, ToSnapshot(), now));
    }

    private void ReplaceShares(IReadOnlyList<(PersonRef Person, Money Amount)> computed)
    {
        // Mutate in place so EF sees updates/inserts/deletes by key, never a delete + re-insert of one key.
        _shares.RemoveAll(s => computed.All(c => c.Person.PersonId != s.PersonId));
        foreach (var (person, share) in computed)
        {
            var existing = _shares.Find(s => s.PersonId == person.PersonId);
            if (existing is null) _shares.Add(new ExpenseShare(Id, person, share));
            else existing.Update(person, share);
        }
    }

    private static string NormalizeReason(string? reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxReasonLength)
            throw new BudgetDomainException($"A reason of 1–{MaxReasonLength} characters is required.");

        return trimmed;
    }

    private static void ValidateFunding(FundingSource funding, PersonRef? paidBy, IReadOnlyCollection<PersonRef> participants)
    {
        if (funding == FundingSource.Individual && paidBy is null)
            throw new BudgetDomainException("An individually funded expense needs a payer.");

        if (funding == FundingSource.HouseholdFunds && (paidBy is not null || participants.Count > 0))
            throw new BudgetDomainException("Household funds have no payer and are not split.");

        if (participants.Select(p => p.PersonId).Distinct().Count() != participants.Count)
            throw new BudgetDomainException("A participant can only be listed once.");
    }

    /// <summary>The budget this expense belongs to.</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary>The envelope it was recorded in (immutable).</summary>
    public BudgetAccountId BudgetAccountId { get; private set; } = default!;

    /// <summary>Amount, two decimals.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Reporting category.</summary>
    public ExpenseCategory Category { get; private set; }

    /// <summary>Purchase date.</summary>
    public DateOnly OccurredOn { get; private set; }

    /// <summary>Where the money came from.</summary>
    public FundingSource FundingSource { get; private set; }

    /// <summary>Whose personal money paid; <see langword="null"/> for household funds.</summary>
    public Guid? PaidByPersonId { get; private set; }

    /// <summary>Payer name snapshot.</summary>
    public string? PaidByDisplayName { get; private set; }

    /// <summary>The human who entered it (immutable).</summary>
    public Guid AddedByPersonId { get; private set; }

    /// <summary>Recorder name snapshot.</summary>
    public string AddedByDisplayName { get; private set; } = default!;

    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Current revision number; 1 after creation.</summary>
    public int Revision { get; private set; }

    /// <summary>Whether the expense was voided; void expenses no longer count and cannot be edited.</summary>
    public bool IsVoided { get; private set; }

    /// <summary>When it was voided, UTC.</summary>
    public DateTime? VoidedAt { get; private set; }

    /// <summary>
    /// Concurrency token mapped to PostgreSQL's <c>xmin</c>; never set by domain logic. Every
    /// update — including share-only ones — rewrites this row, so the check always applies.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>The stored equal shares; empty unless individually funded and shared.</summary>
    public IReadOnlyCollection<ExpenseShare> Shares => _shares;

    /// <summary>Immutable revision history, starting with creation.</summary>
    public IReadOnlyCollection<ExpenseRevision> Revisions => _revisions;

    private ExpenseSnapshot ToSnapshot() => new()
    {
        Amount = Amount,
        Category = Category,
        OccurredOn = OccurredOn,
        FundingSource = FundingSource,
        PaidByPersonId = PaidByPersonId,
        PaidByDisplayName = PaidByDisplayName,
        AddedByPersonId = AddedByPersonId,
        AddedByDisplayName = AddedByDisplayName,
        IsVoided = IsVoided,
        Shares = [.. _shares.Select(s => new ShareSnapshot
        {
            PersonId = s.PersonId,
            PersonDisplayName = s.PersonDisplayName,
            Amount = s.Amount,
        })],
    };
}
