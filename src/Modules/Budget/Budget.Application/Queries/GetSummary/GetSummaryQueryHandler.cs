namespace Budget.Application.Queries.GetSummary;

using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetSummaryQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetSummaryQuery, SummaryDto>
{
    public async Task<SummaryDto> HandleAsync(GetSummaryQuery query, CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();
        if (!BudgetMonth.TryParse(query.Month, out var month))
            errors.Add(new ValidationError(nameof(query.Month), "Month must be YYYY-MM."));
        var scope = query.Scope?.Trim().ToLowerInvariant();
        if (scope is not ("shared" or "personal"))
            errors.Add(new ValidationError(nameof(query.Scope), "Scope must be shared or personal."));
        if (errors.Count > 0)
            throw new CommandValidationException(nameof(GetSummaryQuery), errors);

        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var shared = scope == "shared";
        Guid? owner = null;
        if (shared)
        {
            if (!caller.IsAdult)
                throw new ForbiddenException("Only an owner or adult can see shared spending.");
        }
        else
        {
            owner = query.OwnerPersonId ?? caller.PersonId;
            if (!caller.PersonalOwnerIds.Contains(owner))
                throw new ForbiddenException("You can only see your own spending or a member you manage.");
        }

        var currency = await db.Budgets.AsNoTracking()
            .Where(b => b.HouseholdId == caller.HouseholdId)
            .Select(b => b.Currency)
            .FirstOrDefaultAsync(ct);
        if (currency == default && !await db.Budgets.AnyAsync(b => b.HouseholdId == caller.HouseholdId, ct))
            throw new NotFoundException("Budget", caller.HouseholdId);

        // Visibility first, then scope: another person's private envelope can never be summed.
        var inScope = db.BudgetAccounts.AsNoTracking()
            .Where(a => db.Budgets.Any(b => b.Id == a.BudgetId && b.HouseholdId == caller.HouseholdId))
            .VisibleTo(caller)
            .Where(a => shared
                ? a.Visibility == AccountVisibility.Household
                : a.Visibility == AccountVisibility.Personal && a.OwnerPersonId == owner);
        var accounts = await inScope
            .Select(a => new { a.Id, a.Name, a.Visibility, a.OwnerPersonId, a.IsArchived })
            .ToListAsync(ct);
        var ids = accounts.Select(a => a.Id).ToList();

        var start = month.Start;
        var next = month.NextStart;
        var sums = await db.Expenses.AsNoTracking()
            .Where(e => ids.Contains(e.BudgetAccountId) && !e.IsVoided && e.OccurredOn >= start && e.OccurredOn < next)
            .GroupBy(e => new { e.BudgetAccountId, e.Category })
            .Select(g => new { g.Key.BudgetAccountId, g.Key.Category, Total = g.Sum(e => e.Amount), Count = g.Count() })
            .ToListAsync(ct);
        var limits = await db.MonthlyLimits.AsNoTracking()
            .Where(l => ids.Contains(l.BudgetAccountId) && l.MonthStart == start)
            .Select(l => new { l.BudgetAccountId, l.Amount, l.Revision })
            .ToListAsync(ct);

        var spentByAccount = sums.GroupBy(s => s.BudgetAccountId).ToDictionary(g => g.Key, g => g.Sum(s => s.Total));
        var countByAccount = sums.GroupBy(s => s.BudgetAccountId).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
        var limitByAccount = limits.ToDictionary(l => l.BudgetAccountId);

        static string F(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);

        var envelopes = accounts
            .Select(a =>
            {
                var spent = spentByAccount.GetValueOrDefault(a.Id);
                limitByAccount.TryGetValue(a.Id, out var limit);
                return (Account: a, Spent: spent, Limit: limit);
            })
            .Where(x => !x.Account.IsArchived || x.Spent > 0 || x.Limit is not null)
            .OrderBy(x => x.Account.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Account.Id.Value)
            .Select(x => new SummaryEnvelopeDto(
                x.Account.Id.Value, x.Account.Name, x.Account.Visibility.ToString(), x.Account.OwnerPersonId, x.Account.IsArchived,
                F(x.Spent),
                countByAccount.GetValueOrDefault(x.Account.Id),
                x.Limit is null ? null : F(x.Limit.Amount),
                x.Limit?.Revision,
                x.Limit is null ? null : F(x.Limit.Amount - x.Spent),
                x.Limit is not null && x.Spent > x.Limit.Amount))
            .ToList();

        var categories = sums
            .GroupBy(s => s.Category)
            .Select(g => (Category: g.Key, Total: g.Sum(s => s.Total)))
            .OrderByDescending(c => c.Total).ThenBy(c => c.Category.ToString(), StringComparer.Ordinal)
            .Select(c => new SummaryCategoryDto(c.Category.ToString(), F(c.Total)))
            .ToList();

        return new SummaryDto(month.ToString(), shared ? "Shared" : "Personal", currency.ToString(), F(spentByAccount.Values.Sum()), countByAccount.Values.Sum(), envelopes, categories);
    }
}
