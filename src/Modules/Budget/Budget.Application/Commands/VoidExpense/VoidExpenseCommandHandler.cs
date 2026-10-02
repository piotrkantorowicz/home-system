namespace Budget.Application.Commands.VoidExpense;

using Budget.Application.Commands.CreateExpense;
using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class VoidExpenseCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IExpenseRepository expenses,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<VoidExpenseCommand, ExpenseMutationResult>
{
    public async Task<ExpenseMutationResult> HandleAsync(VoidExpenseCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var (expense, account) = await ExpenseMutationSupport.LoadAuthorizedAsync(caller, budgets, accounts, expenses, command.ExpenseId, ct);

        var request = ExpenseRequest.ForVoid(account.Id.Value, expense.Id.Value, command.ExpectedRevision, command.Reason.Trim());
        var replay = await ExpenseMutationSupport.FindReplayAsync(expenses, caller, expense, command.ClientRequestId, request, ct);
        if (replay is not null)
            return replay;

        // Voiding twice (even with a fresh key) is a completed transition: no second revision.
        if (expense.IsVoided)
            return new ExpenseMutationResult(expense.Id.Value, expense.Revision, Created: false);

        expense.Void(command.ExpectedRevision, command.Reason, caller.Self, command.ClientRequestId, request, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return new ExpenseMutationResult(expense.Id.Value, expense.Revision, Created: false);
    }
}
