namespace Budget.Application.Commands.RenameAccount;

using Budget.Application.Common;
using Budget.Application.Queries.GetAccount;
using Budget.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class RenameAccountCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IBudgetUnitOfWork unitOfWork) : ICommandHandler<RenameAccountCommand, AccountDto>
{
    public async Task<AccountDto> HandleAsync(RenameAccountCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.Id, ct);

        account.Rename(command.Name, command.ExpectedRevision);
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return account.ToAccountDto();
    }
}
