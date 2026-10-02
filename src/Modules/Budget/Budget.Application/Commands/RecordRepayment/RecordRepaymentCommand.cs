namespace Budget.Application.Commands.RecordRepayment;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Records that one person actually paid another. Owner/Adult only, and either party may be named:
/// an adult can record on behalf of someone else. Both must be current owners/adults or people
/// already in this household's shared ledger (so someone who left or was demoted can still be
/// settled). A payment larger than the current suggestion is recorded as stated.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="ClientRequestId">Idempotency key, generated once per logical submission.</param>
/// <param name="FromPersonId">Who paid.</param>
/// <param name="ToPersonId">Who received; must differ from the sender.</param>
/// <param name="Amount">Positive decimal string, at most two decimals.</param>
/// <param name="PaidOn">The date the payment was made.</param>
/// <param name="Note">Optional note, up to 200 characters.</param>
public sealed record RecordRepaymentCommand(
    string AuthSubject,
    Guid ClientRequestId,
    Guid FromPersonId,
    Guid ToPersonId,
    string Amount,
    DateOnly PaidOn,
    string? Note) : ICommand<RepaymentMutationResult>;

/// <summary>Outcome of a record (or its replay).</summary>
/// <param name="RepaymentId">The repayment.</param>
/// <param name="Revision">The revision this request produced (always 1 for a creation).</param>
/// <param name="Created"><see langword="true"/> when this call created it; <see langword="false"/> for a recognised retry.</param>
public sealed record RepaymentMutationResult(Guid RepaymentId, int Revision, bool Created);
