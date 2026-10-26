namespace Budget.Application.Commands.ImportExpenses;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Records reviewed statement rows as expenses in one envelope, all or none (<c>bank-import.md</c> §5–§6).
/// Rows are user-asserted facts like manual entries; the server does not re-read the file. A retry with
/// the same <paramref name="ClientRequestId"/> and rows returns the original result.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="ClientRequestId">Idempotency key of the whole import.</param>
/// <param name="AccountId">The envelope every row is recorded in.</param>
/// <param name="Rows">The selected rows, 1 to <see cref="MaxRows"/>.</param>
public sealed record ImportExpensesCommand(
    string AuthSubject,
    Guid ClientRequestId,
    Guid AccountId,
    IReadOnlyList<ImportExpenseRow> Rows) : ICommand<ImportExpensesResult>
{
    /// <summary>Most rows one import may carry (§8).</summary>
    public const int MaxRows = 1000;
}
