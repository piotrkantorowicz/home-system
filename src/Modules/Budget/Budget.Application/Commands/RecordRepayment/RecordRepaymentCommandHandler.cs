namespace Budget.Application.Commands.RecordRepayment;

using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.Exceptions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class RecordRepaymentCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    ISettlementRepository settlements,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RecordRepaymentCommand, RepaymentMutationResult>
{
    public async Task<RepaymentMutationResult> HandleAsync(RecordRepaymentCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAdultAsync(command.AuthSubject, ct);
        var budget = await budgets.GetByHouseholdAsync(caller.HouseholdId, ct)
            ?? throw new NotFoundException("Budget", caller.HouseholdId);

        // Shape was checked by RecordRepaymentCommandValidator before the handler runs.
        if (!Money.TryParsePositive(command.Amount, out var amount))
            throw new InvalidOperationException("Unvalidated amount.");

        // Authorised above; now recognise a retry before any roster/eligibility validation. A retry
        // after a later void still gets the original result.
        await settlements.LockRequestAsync(budget.Id, caller.PersonId, command.ClientRequestId, ct);
        var previous = await settlements.FindByRequestAsync(budget.Id, caller.PersonId, command.ClientRequestId, ct);
        if (previous is not null)
        {
            return previous.MatchesCreation(command.FromPersonId, command.ToPersonId, amount, command.PaidOn, command.Note)
                ? new RepaymentMutationResult(previous.Id.Value, 1, Created: false)
                : throw new ConflictException("This request ID was already used for a different payment.");
        }

        var from = await ResolveAsync(caller, budget.Id, command.FromPersonId, ct);
        var to = await ResolveAsync(caller, budget.Id, command.ToPersonId, ct);

        var settlement = Settlement.Create(
            SettlementId.New(), budget.Id, from, to, amount, command.PaidOn, command.Note, caller.Self,
            command.ClientRequestId, clock.GetUtcNow().UtcDateTime);

        await settlements.AddAsync(settlement, ct);
        await unitOfWork.CommitAsync(ct);

        return new RepaymentMutationResult(settlement.Id.Value, settlement.Revision, Created: true);
    }

    /// <summary>A current owner/adult, or someone already in this budget's shared ledger; anyone else is an outsider.</summary>
    private async Task<PersonRef> ResolveAsync(BudgetCaller caller, BudgetId budgetId, Guid personId, CancellationToken ct)
    {
        if (caller.FindAdult(personId) is { } adult)
            return adult;

        var stored = await settlements.FindLedgerNameAsync(budgetId, personId, ct)
            ?? throw new BudgetDomainException("Both people must be adults in this household or already in its shared expenses.");
        return new PersonRef(personId, stored);
    }
}
