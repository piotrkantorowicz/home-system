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

        if (funding == FundingSource.Individual && paidBy is null)
            throw new BudgetDomainException("An individually funded expense needs a payer.");

        if (funding == FundingSource.HouseholdFunds && (paidBy is not null || participants.Count > 0))
            throw new BudgetDomainException("Household funds have no payer and are not split.");

        if (participants.Select(p => p.PersonId).Distinct().Count() != participants.Count)
            throw new BudgetDomainException("A participant can only be listed once.");

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
                expense._shares.Add(new ExpenseShare(person, share));

        var request = new ExpenseRequest(
            "Create", account.Id.Value, amount.ToString(), category, occurredOn, funding,
            paidBy?.PersonId, [.. participants.Select(p => p.PersonId).Order()]);
        expense._revisions.Add(new ExpenseRevision(id, account.BudgetId, 1, addedBy.PersonId, clientRequestId, request, expense.ToSnapshot(), now));

        return expense;
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
        Shares = [.. _shares.Select(s => new ShareSnapshot
        {
            PersonId = s.PersonId,
            PersonDisplayName = s.PersonDisplayName,
            Amount = s.Amount,
        })],
    };
}
