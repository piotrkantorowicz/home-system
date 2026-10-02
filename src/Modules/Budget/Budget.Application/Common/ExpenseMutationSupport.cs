namespace Budget.Application.Common;

using Budget.Application.Commands.CreateExpense;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Steps shared by expense update and void: authorise the target, then recognise a retry.</summary>
internal static class ExpenseMutationSupport
{
    /// <summary>
    /// Loads the expense in the caller's budget. Missing, other-household and invisible expenses are
    /// all not-found.
    /// </summary>
    public static async Task<(Expense Expense, BudgetAccount Account)> LoadAuthorizedAsync(
        BudgetCaller caller, IBudgetRepository budgets, IBudgetAccountRepository accounts, IExpenseRepository expenses,
        Guid expenseId, CancellationToken ct)
    {
        await expenses.LockExpenseAsync(expenseId, ct);
        var budget = await budgets.GetByHouseholdAsync(caller.HouseholdId, ct)
            ?? throw new NotFoundException("Expense", expenseId);
        var expense = await expenses.GetAsync(ExpenseId.From(expenseId), budget.Id, ct)
            ?? throw new NotFoundException("Expense", expenseId);
        var account = await accounts.GetAsync(expense.BudgetAccountId, budget.Id, ct);

        return account is not null && caller.CanAccess(account)
            ? (expense, account)
            : throw new NotFoundException("Expense", expenseId);
    }

    /// <summary>
    /// Serialises this request identity, then returns the original result when it was already
    /// committed (before any stale-revision or roster validation); a reused key with a different
    /// request is a conflict. <see langword="null"/> means the request is new.
    /// </summary>
    public static async Task<ExpenseMutationResult?> FindReplayAsync(
        IExpenseRepository expenses, BudgetCaller caller, Expense expense, Guid clientRequestId, ExpenseRequest request, CancellationToken ct)
    {
        await expenses.LockRequestAsync(expense.BudgetId, caller.PersonId, clientRequestId, ct);
        var previous = await expenses.FindRevisionByRequestAsync(expense.BudgetId, caller.PersonId, clientRequestId, ct);
        if (previous is null)
            return null;

        return previous.Request.Matches(request)
            ? new ExpenseMutationResult(previous.ExpenseId.Value, previous.RevisionNumber, Created: false)
            : throw new ConflictException("This request ID was already used for a different request.");
    }
}
