namespace Budget.Application.Commands.ClearMonthlyLimit;

using Budget.Application.Common;
using Budget.Domain.Abstractions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class ClearMonthlyLimitCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IMonthlyLimitRepository limits,
    IBudgetUnitOfWork unitOfWork) : ICommandHandler<ClearMonthlyLimitCommand>
{
    public async Task HandleAsync(ClearMonthlyLimitCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.AccountId, ct);

        if (!BudgetMonth.TryParse(command.Month, out var month))
            throw new InvalidOperationException("Unvalidated month.");

        await limits.LockAsync(account.Id, month, ct);
        var existing = await limits.GetAsync(account.Id, month, ct);
        if (existing is null)
            return;

        existing.EnsureRevision(command.ExpectedRevision);
        limits.Remove(existing);
        await unitOfWork.CommitOrThrowConflictAsync(ct);
    }
}
