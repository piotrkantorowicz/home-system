namespace Budget.Application.Commands.CreateAccount;

using Budget.Application.Common;
using Budget.Application.Queries.GetAccount;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateAccountCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateAccountCommand, AccountDto>
{
    public async Task<AccountDto> HandleAsync(CreateAccountCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var visibility = Enum.Parse<AccountVisibility>(command.Visibility.Trim(), ignoreCase: true); // validated

        Guid? owner = null;
        if (visibility == AccountVisibility.Household)
        {
            if (!caller.IsAdult)
                throw new ForbiddenException("Only an owner or adult can create a household envelope.");
        }
        else
        {
            owner = command.OwnerPersonId ?? caller.PersonId;
            if (!caller.PersonalOwnerIds.Contains(owner))
                throw new ForbiddenException("You can only create a personal envelope for yourself or a member you manage.");
        }

        var budget = await budgets.GetByHouseholdAsync(caller.HouseholdId, ct)
            ?? throw new NotFoundException("Budget", caller.HouseholdId);

        var account = BudgetAccount.Create(
            BudgetAccountId.New(), budget.Id, command.Name, visibility, owner, clock.GetUtcNow().UtcDateTime);

        await accounts.AddAsync(account, ct);
        await unitOfWork.CommitAsync(ct);

        return account.ToAccountDto();
    }
}
