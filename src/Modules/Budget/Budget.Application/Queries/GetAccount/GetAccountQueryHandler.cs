namespace Budget.Application.Queries.GetAccount;

using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetAccountQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> HandleAsync(GetAccountQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var id = BudgetAccountId.From(query.Id);

        return await db.BudgetAccounts.AsNoTracking()
            .Where(a => a.Id == id
                && db.Budgets.Any(b => b.Id == a.BudgetId && b.HouseholdId == caller.HouseholdId))
            .VisibleTo(caller)
            .Select(AccountVisibilityFilter.ToDto)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Envelope", query.Id);
    }
}
