namespace Budget.Application.Queries.ListExpenses;

using System.Globalization;
using Budget.Application.Commands.CreateExpense;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class ListExpensesQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<ListExpensesQuery, ExpenseListDto>
{
    private const int MaxPageSize = 100;

    public async Task<ExpenseListDto> HandleAsync(ListExpensesQuery query, CancellationToken ct = default)
    {
        var errors = Validate(query, out var category, out var amount);
        if (errors.Count > 0)
            throw new CommandValidationException(nameof(ListExpensesQuery), errors);

        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var expenses = db.VisibleTo(caller);
        if (!query.IncludeVoided) expenses = expenses.Where(e => !e.IsVoided);
        if (query.AccountId is { } accountId)
        {
            var id = BudgetAccountId.From(accountId);
            expenses = expenses.Where(e => e.BudgetAccountId == id);
        }

        if (category is { } c) expenses = expenses.Where(e => e.Category == c);
        if (amount is { } a) expenses = expenses.Where(e => e.Amount == a.Amount);
        if (query.From is { } from) expenses = expenses.Where(e => e.OccurredOn >= from);
        if (query.To is { } to) expenses = expenses.Where(e => e.OccurredOn <= to);
        if (query.Search?.Trim() is { Length: > 0 } search)
        {
            var pattern = $"%{search.ToLowerInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
#pragma warning disable CA1304, CA1311 // translated to SQL lower(); EF cannot translate ToLowerInvariant or a culture overload
            expenses = expenses.Where(e => e.Description != null && EF.Functions.Like(e.Description.ToLower(), pattern, "\\"));
#pragma warning restore CA1304, CA1311
        }

        if (query.ExcludeId is { } exclude)
        {
            var id = ExpenseId.From(exclude);
            expenses = expenses.Where(e => e.Id != id);
        }

        var total = await expenses.CountAsync(ct);

        // Aggregates cover every page of the filtered set; a voided expense never counts.
        var active = expenses.Where(e => !e.IsVoided);
        var days = await active
            .GroupBy(e => e.OccurredOn)
            .Select(g => new { Date = g.Key, Total = g.Sum(e => e.Amount), Count = g.Count() })
            .OrderByDescending(d => d.Date)
            .ToListAsync(ct);
        var callerId = caller.PersonId;
        var yourShare = await active
            .SelectMany(e => e.Shares.Where(s => s.PersonId == callerId))
            .SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;

        var rows = await expenses
            .OrderByDescending(e => e.OccurredOn).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ExpenseReadExtensions.ToRow)
            .ToListAsync(ct);

        static string F(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);

        return new ExpenseListDto(
            [.. rows.Select(r => r.ToDto(caller))], total, page, pageSize,
            days.Sum(d => d.Count), F(days.Sum(d => d.Total)), F(yourShare),
            [.. days.Select(d => new ExpenseDayTotalDto(d.Date, F(d.Total), d.Count))]);
    }

    private static List<ValidationError> Validate(ListExpensesQuery query, out ExpenseCategory? category, out Money? amount)
    {
        var errors = new List<ValidationError>();
        category = null;
        amount = null;

        if (query.Category is not null)
        {
            if (CreateExpenseCommandValidator.TryParseCategory(query.Category, out var parsed)) category = parsed;
            else errors.Add(new ValidationError(nameof(query.Category), "Unknown category."));
        }

        if (query.Amount is not null)
        {
            if (Money.TryParsePositive(query.Amount, out var parsed)) amount = parsed;
            else errors.Add(new ValidationError(nameof(query.Amount), "Amount must be a positive number with at most two decimals."));
        }

        if (query.From > query.To)
            errors.Add(new ValidationError(nameof(query.From), "From must not be after To."));

        return errors;
    }
}
