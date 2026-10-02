namespace Budget.Application.Commands.SetMonthlyLimit;

using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Queries.ListLimits;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class SetMonthlyLimitCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IMonthlyLimitRepository limits,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<SetMonthlyLimitCommand, MonthlyLimitDto>
{
    public async Task<MonthlyLimitDto> HandleAsync(SetMonthlyLimitCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.AccountId, ct);

        // Shape was checked by SetMonthlyLimitCommandValidator before the handler runs.
        if (!BudgetMonth.TryParse(command.Month, out var month) || !Money.TryParseNonNegative(command.Amount, out var amount))
            throw new InvalidOperationException("Unvalidated limit.");
        var now = clock.GetUtcNow().UtcDateTime;

        // Queue concurrent writers of this envelope/month; the loser then sees the winner's row.
        await limits.LockAsync(account.Id, month, ct);
        var existing = await limits.GetAsync(account.Id, month, ct);

        MonthlyLimit limit;
        if (existing is null)
        {
            if (command.ExpectedRevision is not null)
                throw new ConflictException("This limit no longer exists. Reload and try again.");

            limit = MonthlyLimit.Create(MonthlyLimitId.New(), account, month, amount, now);
            await limits.AddAsync(limit, ct);
        }
        else
        {
            if (command.ExpectedRevision is not { } expected)
                throw new ConflictException("A limit is already set for this month. Reload and try again.");

            existing.Change(account, amount, expected, now);
            limit = existing;
        }

        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return new MonthlyLimitDto(account.Id.Value, month.ToString(), limit.Amount.ToString("F2", CultureInfo.InvariantCulture), limit.Revision);
    }
}
