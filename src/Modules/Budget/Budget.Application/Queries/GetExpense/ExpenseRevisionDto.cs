namespace Budget.Application.Queries.GetExpense;

/// <summary>One immutable step of an expense's history; adjacent revisions show before and after.</summary>
/// <param name="RevisionNumber">1 for creation, then increasing.</param>
/// <param name="Operation"><c>Create</c>, <c>Update</c> or <c>Void</c>.</param>
/// <param name="ActorPersonId">Who made the change.</param>
/// <param name="ActorDisplayName">Name recorded when the change was made.</param>
/// <param name="Reason">Why; <see langword="null"/> for creation.</param>
/// <param name="CreatedAt">When, UTC.</param>
/// <param name="Snapshot">The resulting state.</param>
public sealed record ExpenseRevisionDto(
    int RevisionNumber,
    string Operation,
    Guid ActorPersonId,
    string ActorDisplayName,
    string? Reason,
    DateTime CreatedAt,
    ExpenseSnapshotDto Snapshot);
