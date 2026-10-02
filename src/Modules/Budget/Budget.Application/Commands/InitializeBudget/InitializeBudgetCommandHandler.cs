namespace Budget.Application.Commands.InitializeBudget;

using Budget.Application.Common;
using Budget.Application.Queries.GetBudget;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class InitializeBudgetCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<InitializeBudgetCommand, InitializeBudgetResult>
{
    private const string DefaultAccountName = "Everyday";

    public async Task<InitializeBudgetResult> HandleAsync(InitializeBudgetCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAdultAsync(command.AuthSubject, ct);
        var currency = command.Currency is null ? BudgetCurrency.PLN : ParseCurrency(command.Currency);

        // Concurrent first requests queue here; the loser then finds the winner's committed budget.
        await budgets.LockHouseholdAsync(caller.HouseholdId, ct);

        var existing = await budgets.GetByHouseholdAsync(caller.HouseholdId, ct);
        if (existing is not null)
        {
            if (existing.Currency != currency)
                throw new ConflictException($"This household's budget already uses {existing.Currency}.");

            return new InitializeBudgetResult(new BudgetDto(existing.Id.Value, existing.Currency.ToString()), Created: false);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var budget = BudgetAggregate.Create(BudgetId.New(), caller.HouseholdId, currency, now);
        var account = BudgetAccount.Create(
            BudgetAccountId.New(), budget.Id, DefaultAccountName, AccountVisibility.Household, ownerPersonId: null, now);

        await budgets.AddAsync(budget, ct);
        await accounts.AddAsync(account, ct);
        await unitOfWork.CommitAsync(ct);

        return new InitializeBudgetResult(new BudgetDto(budget.Id.Value, currency.ToString()), Created: true);
    }

    internal static bool TryParseCurrency(string value, out BudgetCurrency currency)
        => Enum.TryParse(value.Trim(), ignoreCase: true, out currency)
           && Enum.IsDefined(currency)
           && value.Trim().All(char.IsLetter); // rejects numeric enum values such as "1"

    private static BudgetCurrency ParseCurrency(string value)
        => TryParseCurrency(value, out var currency) ? currency : throw new ArgumentOutOfRangeException(nameof(value));
}
