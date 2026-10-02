namespace Budget.Domain.Entities;

using Budget.Domain.ValueObjects;

/// <summary>
/// Immutable audit record of one expense change, starting with creation as revision 1. Holds the
/// resulting state and the normalised request that produced it; unique per
/// (budget, actor, client request ID) so retries are recognised.
/// </summary>
public sealed class ExpenseRevision
{
    private ExpenseRevision() { }

    internal ExpenseRevision(
        ExpenseId expenseId, BudgetId budgetId, int revisionNumber, Guid actorPersonId, Guid clientRequestId,
        ExpenseRequest request, ExpenseSnapshot snapshot, DateTime createdAt)
    {
        Id = Guid.CreateVersion7();
        ExpenseId = expenseId;
        BudgetId = budgetId;
        RevisionNumber = revisionNumber;
        Operation = request.Action;
        ActorPersonId = actorPersonId;
        ClientRequestId = clientRequestId;
        Request = request;
        Snapshot = snapshot;
        CreatedAt = createdAt;
    }

    /// <summary>Row identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>The expense it belongs to.</summary>
    public ExpenseId ExpenseId { get; private set; } = default!;

    /// <summary>The budget (repeated for the request-identity unique key).</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary>1 for creation, then increasing.</summary>
    public int RevisionNumber { get; private set; }

    /// <summary>What happened, e.g. <c>Create</c>.</summary>
    public string Operation { get; private set; } = default!;

    /// <summary>Who submitted it.</summary>
    public Guid ActorPersonId { get; private set; }

    /// <summary>Client-generated idempotency key.</summary>
    public Guid ClientRequestId { get; private set; }

    /// <summary>The normalised request.</summary>
    public ExpenseRequest Request { get; private set; } = default!;

    /// <summary>The state this revision produced.</summary>
    public ExpenseSnapshot Snapshot { get; private set; } = default!;

    /// <summary>When it was written, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
}
