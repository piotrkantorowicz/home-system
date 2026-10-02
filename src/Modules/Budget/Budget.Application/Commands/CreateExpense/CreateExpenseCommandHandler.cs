namespace Budget.Application.Commands.CreateExpense;

using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.Exceptions;
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

        var request = new ExpenseRequest(
            "Create", account.Id.Value, amount.ToString(), category, command.OccurredOn, funding, payerId, participantIds);

        // Authorised above; now recognise a retry before any current-roster or archive validation.
        await expenses.LockRequestAsync(account.BudgetId, caller.PersonId, command.ClientRequestId, ct);
        var previous = await expenses.FindRevisionByRequestAsync(account.BudgetId, caller.PersonId, command.ClientRequestId, ct);
        if (previous is not null)
        {
            return previous.Request.Matches(request)
                ? new ExpenseMutationResult(previous.ExpenseId.Value, previous.RevisionNumber, Created: false)
                : throw new ConflictException("This request ID was already used for a different request.");
        }

        var (paidBy, participants) = isPersonal
            ? ResolvePersonal(caller, account, command, funding)
            : ResolveShared(caller, command, funding);

        var expense = Expense.Create(
            ExpenseId.New(), account, amount, category, command.OccurredOn, funding, paidBy, caller.Self, participants,
            command.ClientRequestId, clock.GetUtcNow().UtcDateTime);

        await expenses.AddAsync(expense, ct);
        await unitOfWork.CommitAsync(ct);

        return new ExpenseMutationResult(expense.Id.Value, expense.Revision, Created: true);
    }

    private static (PersonRef?, List<PersonRef>) ResolvePersonal(
        BudgetCaller caller, BudgetAccount account, CreateExpenseCommand command, FundingSource funding)
    {
        if (funding != FundingSource.Individual)
            throw new BudgetDomainException("A personal envelope is funded individually.");
        if (command.PaidByPersonId is { } p && p != account.OwnerPersonId)
            throw new BudgetDomainException("A personal expense is paid by the envelope's owner.");
        if (command.ParticipantIds is { Count: > 0 })
            throw new BudgetDomainException("A personal expense is not shared.");

        var owner = caller.FindMember(account.OwnerPersonId!.Value)
            ?? throw new NotFoundException("Envelope", account.Id.Value);
        return (owner, []);
    }

    private static (PersonRef?, List<PersonRef>) ResolveShared(BudgetCaller caller, CreateExpenseCommand command, FundingSource funding)
    {
        var ids = command.ParticipantIds ?? [];
        if (funding == FundingSource.HouseholdFunds)
            return (command.PaidByPersonId is null ? null : throw new BudgetDomainException("Household funds have no payer."),
                ids.Count == 0 ? [] : throw new BudgetDomainException("Household funds are not split."));

        var payer = command.PaidByPersonId is { } payerId
            ? caller.FindAdult(payerId) ?? throw new BudgetDomainException("The payer must be a current owner or adult.")
            : throw new BudgetDomainException("An individually funded expense needs a payer.");

        if (ids.Count == 0)
            throw new BudgetDomainException("Choose at least one adult to share the cost.");

        var participants = ids
            .Select(id => caller.FindAdult(id) ?? throw new BudgetDomainException("Participants must be current owners or adults."))
            .ToList();
        return (payer, participants);
    }
}
