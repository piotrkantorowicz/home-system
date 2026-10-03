namespace Budget.Application.Commands.UpdateExpense;

using Budget.Application.Commands.CreateExpense;
using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.Exceptions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateExpenseCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IExpenseRepository expenses,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateExpenseCommand, ExpenseMutationResult>
{
    public async Task<ExpenseMutationResult> HandleAsync(UpdateExpenseCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var (expense, account) = await ExpenseMutationSupport.LoadAuthorizedAsync(caller, budgets, accounts, expenses, command.ExpenseId, ct);

        // Shape was checked by UpdateExpenseCommandValidator before the handler runs.
        if (!Money.TryParsePositive(command.Amount, out var amount))
            throw new InvalidOperationException("Unvalidated amount.");
        var category = Enum.Parse<ExpenseCategory>(command.Category.Trim(), ignoreCase: true);
        var funding = command.FundingSource is null
            ? FundingSource.Individual
            : Enum.Parse<FundingSource>(command.FundingSource.Trim(), ignoreCase: true);

        var isPersonal = account.Visibility == AccountVisibility.Personal;
        var participantIds = (command.ParticipantIds ?? []).Order().ToList();
        var description = Expense.NormalizeDescription(command.Description);
        var request = new ExpenseRequest(
            "Update", account.Id.Value, amount.ToString(), category, command.OccurredOn, funding,
            isPersonal ? account.OwnerPersonId : command.PaidByPersonId, participantIds,
            expense.Id.Value, command.ExpectedRevision, command.Reason.Trim(), description);

        var replay = await ExpenseMutationSupport.FindReplayAsync(expenses, caller, expense, command.ClientRequestId, request, ct);
        if (replay is not null)
            return replay;

        var (paidBy, participants) = isPersonal
            ? ResolvePersonal(caller, account, command, funding)
            : ResolveShared(caller, expense, command, funding);

        expense.Update(
            command.ExpectedRevision, command.Reason, amount, category, command.OccurredOn, funding, paidBy, participants,
            caller.Self, command.ClientRequestId, request, clock.GetUtcNow().UtcDateTime, description);

        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return new ExpenseMutationResult(expense.Id.Value, expense.Revision, Created: false);
    }

    private static (PersonRef?, List<PersonRef>) ResolvePersonal(
        BudgetCaller caller, BudgetAccount account, UpdateExpenseCommand command, FundingSource funding)
    {
        if (funding != FundingSource.Individual)
            throw new BudgetDomainException("A personal envelope is funded individually.");
        if (command.PaidByPersonId is { } p && p != account.OwnerPersonId)
            throw new BudgetDomainException("A personal expense is paid by the envelope's owner.");
        if (command.ParticipantIds is { Count: > 0 })
            throw new BudgetDomainException("A personal expense is not shared.");

        return (caller.FindMember(account.OwnerPersonId!.Value), []);
    }

    private static (PersonRef?, List<PersonRef>) ResolveShared(
        BudgetCaller caller, Expense expense, UpdateExpenseCommand command, FundingSource funding)
    {
        var ids = command.ParticipantIds ?? [];
        if (funding == FundingSource.HouseholdFunds)
            return (command.PaidByPersonId is null ? null : throw new BudgetDomainException("Household funds have no payer."),
                ids.Count == 0 ? [] : throw new BudgetDomainException("Household funds are not split."));

        if (command.PaidByPersonId is not { } payerId)
            throw new BudgetDomainException("An individually funded expense needs a payer.");
        if (ids.Count == 0)
            throw new BudgetDomainException("Choose at least one adult to share the cost.");

        // A person who left (or was demoted) may stay only where they already were on this expense.
        var payer = caller.FindAdult(payerId)
            ?? (payerId == expense.PaidByPersonId ? new PersonRef(payerId, expense.PaidByDisplayName ?? "") : null)
            ?? throw new BudgetDomainException("The payer must be a current owner or adult.");

        var participants = ids
            .Select(id => caller.FindAdult(id)
                ?? (expense.Shares.FirstOrDefault(s => s.PersonId == id) is { } s ? new PersonRef(id, s.PersonDisplayName) : null)
                ?? throw new BudgetDomainException("Participants must be current owners or adults."))
            .ToList();
        return (payer, participants);
    }
}
