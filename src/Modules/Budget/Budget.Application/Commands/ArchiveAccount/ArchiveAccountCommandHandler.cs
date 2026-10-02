namespace Budget.Application.Commands.ArchiveAccount;

using Budget.Application.Common;
using Budget.Application.Queries.GetAccount;
using Budget.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class ArchiveAccountCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IBudgetUnitOfWork unitOfWork) : ICommandHandler<ArchiveAccountCommand, AccountDto>
{
    public async Task<AccountDto> HandleAsync(ArchiveAccountCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.Id, ct);

        account.Archive(command.ExpectedRevision);
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return account.ToAccountDto();
    }
}
