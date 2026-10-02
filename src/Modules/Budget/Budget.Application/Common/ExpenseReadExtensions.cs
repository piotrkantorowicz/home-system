namespace Budget.Application.Common;

using System.Globalization;
using System.Linq.Expressions;
using Budget.Application.Persistence;
using Budget.Application.Queries.GetExpense;
using Budget.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

/// <summary>Visibility and projection shared by every expense read.</summary>
internal static class ExpenseReadExtensions
{
    /// <summary>
    /// Expenses in envelopes the caller may see — applied first, so hidden envelopes never reach a
    /// filter, a count, a page or a duplicate match. Archived envelopes keep their history visible.
    /// </summary>
    public static IQueryable<Expense> VisibleTo(this IBudgetReadDbContext db, BudgetCaller caller)
    {
        var accountIds = db.BudgetAccounts
            .Where(a => db.Budgets.Any(b => b.Id == a.BudgetId && b.HouseholdId == caller.HouseholdId))
            .VisibleTo(caller)
            .Select(a => a.Id);

        return db.Expenses.AsNoTracking().Where(e => accountIds.Contains(e.BudgetAccountId));
    }

    public static readonly Expression<Func<Expense, ExpenseRow>> ToRow = e => new ExpenseRow(
        e.Id.Value, e.BudgetAccountId.Value, e.Amount, e.Category.ToString(), e.OccurredOn, e.FundingSource.ToString(),
        e.PaidByPersonId, e.PaidByDisplayName, e.AddedByPersonId, e.AddedByDisplayName, e.Revision, e.CreatedAt,
        e.Shares.OrderBy(s => s.PersonId).Select(s => new ShareRow(s.PersonId, s.PersonDisplayName, s.Amount)).ToList());

    /// <summary>Maps a row to its DTO: current roster names for members, stored snapshots for everyone else.</summary>
    public static ExpenseDto ToDto(this ExpenseRow r, BudgetCaller caller)
    {
        string Name(Guid id, string stored) => caller.Members.FirstOrDefault(m => m.PersonId == id)?.DisplayName ?? stored;

        return new ExpenseDto(
            r.Id, r.AccountId, r.Amount.ToString("F2", CultureInfo.InvariantCulture), r.Category, r.OccurredOn, r.FundingSource,
            r.PaidByPersonId, r.PaidByPersonId is { } payer ? Name(payer, r.PaidByDisplayName ?? "") : null,
            r.AddedByPersonId, Name(r.AddedByPersonId, r.AddedByDisplayName), r.Revision, r.CreatedAt,
            [.. r.Shares.Select(s => new ExpenseShareDto(s.PersonId, Name(s.PersonId, s.DisplayName), s.Amount.ToString("F2", CultureInfo.InvariantCulture)))]);
    }
}

internal sealed record ShareRow(Guid PersonId, string DisplayName, decimal Amount);

internal sealed record ExpenseRow(
    Guid Id, Guid AccountId, decimal Amount, string Category, DateOnly OccurredOn, string FundingSource,
    Guid? PaidByPersonId, string? PaidByDisplayName, Guid AddedByPersonId, string AddedByDisplayName,
    int Revision, DateTime CreatedAt, List<ShareRow> Shares);
