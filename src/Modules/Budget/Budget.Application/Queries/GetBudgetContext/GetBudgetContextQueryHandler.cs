namespace Budget.Application.Queries.GetBudgetContext;

using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Application.Queries.GetAccount;
using Budget.Application.Queries.GetBudget;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetBudgetContextQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetBudgetContextQuery, BudgetContextDto>
{
    private static readonly IReadOnlyList<string> Categories = Enum.GetNames<ExpenseCategory>();
    private static readonly IReadOnlyList<string> FundingSources = Enum.GetNames<FundingSource>();

    public async Task<BudgetContextDto> HandleAsync(GetBudgetContextQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);

        var budget = await db.Budgets.AsNoTracking()
            .Where(b => b.HouseholdId == caller.HouseholdId)
            .Select(b => new { b.Id, b.Currency })
            .FirstOrDefaultAsync(ct);

        // ponytail: every active envelope, unpaged; a household has a handful. Page it if that stops holding.
        IReadOnlyList<AccountDto> accounts = budget is null
            ? []
            : await db.BudgetAccounts.AsNoTracking()
                .Where(a => a.BudgetId == budget.Id && !a.IsArchived)
                .VisibleTo(caller)
                .OrderBy(a => a.Name).ThenBy(a => a.Id)
                .Select(AccountVisibilityFilter.ToDto)
                .ToListAsync(ct);

        List<BudgetParticipantDto> participants = caller.Adults
            .OrderBy(m => m.DisplayName, StringComparer.Ordinal).ThenBy(m => m.PersonId)
            .Select(m => new BudgetParticipantDto(m.PersonId, m.DisplayName))
            .ToList();

        return new BudgetContextDto(
            budget is null ? null : new BudgetDto(budget.Id.Value, budget.Currency.ToString()),
            accounts, Categories, FundingSources, participants);
    }
}
