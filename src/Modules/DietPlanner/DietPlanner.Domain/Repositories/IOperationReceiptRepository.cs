namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;

/// <summary>
/// Store of <see cref="OperationReceipt"/>s. Claims run inside the command's ambient transaction, so a
/// receipt commits or rolls back together with the operation's data.
/// </summary>
public interface IOperationReceiptRepository
{
    /// <summary>Inserts the receipt unless one exists for the same caller, operation and key. A concurrent claim of the same key waits for the other transaction and then loses.</summary>
    /// <param name="receipt">The receipt to insert.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>True when this call inserted it; false when a receipt already existed.</returns>
    Task<bool> TryClaimAsync(OperationReceipt receipt, CancellationToken ct = default);

    /// <summary>Finds the receipt for a caller's keyed operation.</summary>
    /// <param name="actorSubject">Auth subject of the caller.</param>
    /// <param name="operation">Operation name.</param>
    /// <param name="key">Idempotency key.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The receipt, or <see langword="null"/>.</returns>
    Task<OperationReceipt?> FindAsync(string actorSubject, string operation, string key, CancellationToken ct = default);
}
