namespace Budget.Application.Commands.RestoreAccount;

using Budget.Application.Common;
using Budget.Application.Queries.GetAccount;
using Budget.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class RestoreAccountCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IBudgetUnitOfWork unitOfWork) : ICommandHandler<RestoreAccountCommand, AccountDto>
{
    public async Task<AccountDto> HandleAsync(RestoreAccountCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.Id, ct);

        account.Restore(command.ExpectedRevision);
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return account.ToAccountDto();
    }
}
