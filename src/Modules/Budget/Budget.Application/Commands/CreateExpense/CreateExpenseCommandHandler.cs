namespace Budget.Application.Commands.CreateExpense;

using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateExpenseCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IExpenseRepository expenses,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateExpenseCommand, ExpenseMutationResult>
{
    public async Task<ExpenseMutationResult> HandleAsync(CreateExpenseCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.AccountId, ct);

        // Shape was checked by CreateExpenseCommandValidator before the handler runs.
        if (!Money.TryParsePositive(command.Amount, out var amount))
            throw new InvalidOperationException("Unvalidated amount.");
        var category = Enum.Parse<ExpenseCategory>(command.Category.Trim(), ignoreCase: true);
        var funding = command.FundingSource is null
            ? FundingSource.Individual
            : Enum.Parse<FundingSource>(command.FundingSource.Trim(), ignoreCase: true);

        var isPersonal = account.Visibility == AccountVisibility.Personal;
        var participantIds = (command.ParticipantIds ?? []).Order().ToList();
        var payerId = isPersonal ? account.OwnerPersonId : command.PaidByPersonId;

        var description = Expense.NormalizeDescription(command.Description);
        var request = new ExpenseRequest(
            "Create", account.Id.Value, amount.ToString(), category, command.OccurredOn, funding, payerId, participantIds,
            Description: description);

        // Authorised above; now recognise a retry before any current-roster or archive validation.
        await expenses.LockRequestAsync(account.BudgetId, caller.PersonId, command.ClientRequestId, ct);
        var previous = await expenses.FindRevisionByRequestAsync(account.BudgetId, caller.PersonId, command.ClientRequestId, ct);
        if (previous is not null)
        {
            return previous.Request.Matches(request)
                ? new ExpenseMutationResult(previous.ExpenseId.Value, previous.RevisionNumber, Created: false)
                : throw new ConflictException("This request ID was already used for a different request.");
        }

        var (paidBy, participants) = ExpenseParties.Resolve(caller, account, funding, command.PaidByPersonId, command.ParticipantIds);

        var expense = Expense.Create(
            ExpenseId.New(), account, amount, category, command.OccurredOn, funding, paidBy, caller.Self, participants,
            command.ClientRequestId, clock.GetUtcNow().UtcDateTime, description);

        await expenses.AddAsync(expense, ct);
        await unitOfWork.CommitAsync(ct);

        return new ExpenseMutationResult(expense.Id.Value, expense.Revision, Created: true);
    }
}
