namespace Budget.Application.Commands.VoidRepayment;

using Budget.Application.Commands.RecordRepayment;
using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class VoidRepaymentCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    ISettlementRepository settlements,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<VoidRepaymentCommand, RepaymentMutationResult>
{
    public async Task<RepaymentMutationResult> HandleAsync(VoidRepaymentCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAdultAsync(command.AuthSubject, ct);

        // Queue concurrent voids; the loser then loads the committed state and sees it already void.
        await settlements.LockSettlementAsync(command.RepaymentId, ct);
        var budget = await budgets.GetByHouseholdAsync(caller.HouseholdId, ct)
            ?? throw new NotFoundException("Repayment", command.RepaymentId);
        var settlement = await settlements.GetAsync(SettlementId.From(command.RepaymentId), budget.Id, ct)
            ?? throw new NotFoundException("Repayment", command.RepaymentId);

        if (settlement.IsVoided)
            return new RepaymentMutationResult(settlement.Id.Value, settlement.Revision, Created: false);

        settlement.Void(command.ExpectedRevision, command.Reason, caller.Self, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return new RepaymentMutationResult(settlement.Id.Value, settlement.Revision, Created: false);
    }
}
