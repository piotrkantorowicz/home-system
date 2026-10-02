namespace Budget.Application.Commands.VoidRepayment;

using Budget.Application.Commands.RecordRepayment;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Voids a recorded repayment so the debt it reduced is restored. Owner/Adult only. Completing a
/// void that is already complete is a no-op that adds nothing.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="RepaymentId">The repayment.</param>
/// <param name="ExpectedRevision">The revision the caller last saw; a mismatch is a 409.</param>
/// <param name="Reason">Short reason, 1–200 characters.</param>
public sealed record VoidRepaymentCommand(string AuthSubject, Guid RepaymentId, int ExpectedRevision, string Reason)
    : ICommand<RepaymentMutationResult>;
