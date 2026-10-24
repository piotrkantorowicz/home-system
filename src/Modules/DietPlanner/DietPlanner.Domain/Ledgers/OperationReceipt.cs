namespace DietPlanner.Domain.Ledgers;

/// <summary>
/// Durable record that a caller's keyed write operation committed, so a retried request (lost
/// response, transport retry, restart) returns the original result instead of writing again.
/// Written in the same transaction as the operation's data. Receipts are kept indefinitely: pruning
/// one would let an old key silently write again.
/// </summary>
public sealed class OperationReceipt
{
    private OperationReceipt() { }

    /// <summary>Creates a receipt for a keyed operation.</summary>
    /// <param name="actorSubject">Auth subject of the caller; keys are scoped per caller.</param>
    /// <param name="operation">Operation name, e.g. <c>create_product</c>; keys are scoped per operation.</param>
    /// <param name="key">Caller-chosen idempotency key.</param>
    /// <param name="requestHash">Hash of the canonical request; a different request under the same key is a conflict.</param>
    /// <param name="resultId">Id the operation produced, returned on replay.</param>
    /// <param name="createdAtUtc">Commit time, UTC.</param>
    public static OperationReceipt Create(
        string actorSubject, string operation, string key, string requestHash, Guid resultId, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        return new OperationReceipt
        {
            ActorSubject = actorSubject,
            Operation = operation,
            Key = key,
            RequestHash = requestHash,
            ResultId = resultId,
            CreatedAt = createdAtUtc,
        };
    }

    /// <summary>Auth subject of the caller.</summary>
    public string ActorSubject { get; private init; } = default!;
    /// <summary>Operation name.</summary>
    public string Operation { get; private init; } = default!;
    /// <summary>Caller-chosen idempotency key.</summary>
    public string Key { get; private init; } = default!;
    /// <summary>SHA-256 of the canonical request, hex.</summary>
    public string RequestHash { get; private init; } = default!;
    /// <summary>Id the operation produced.</summary>
    public Guid ResultId { get; private init; }
    /// <summary>Commit time, UTC.</summary>
    public DateTime CreatedAt { get; private init; }
}
