namespace Budget.Application.Queries.PreviewStatementImport;

using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Application.StatementImports;
using Budget.Domain.Aggregates;
using Budget.Domain.Exceptions;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Classifies each statement row as a candidate (<c>New</c>, <c>AlreadyImported</c>,
/// <c>PossibleDuplicate</c>) or a skipped row (<c>bank-import.md</c> §3–§4). Read-only.
/// </summary>
internal sealed class PreviewStatementImportQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<PreviewStatementImportQuery, StatementPreviewDto>
{
    private const int DuplicateWindowDays = 2;
    private const int MaxMatches = 5;

    public async Task<StatementPreviewDto> HandleAsync(PreviewStatementImportQuery query, CancellationToken ct = default)
    {
        if (query.Content.Length > PreviewStatementImportQuery.MaxFileBytes)
            throw new CommandValidationException(nameof(PreviewStatementImportQuery),
                [new ValidationError("File", $"A statement file can be at most {PreviewStatementImportQuery.MaxFileBytes / 1024 / 1024} MiB.")]);

        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var budget = await db.Budgets.AsNoTracking()
            .Where(b => b.HouseholdId == caller.HouseholdId)
            .Select(b => new { b.Id, b.Currency })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Envelope", query.BudgetAccountId);

        var accountId = BudgetAccountId.From(query.BudgetAccountId);
        var account = await db.BudgetAccounts.AsNoTracking()
            .Where(a => a.BudgetId == budget.Id && a.Id == accountId)
            .VisibleTo(caller)
            .Select(a => new { a.Visibility, a.OwnerPersonId, a.IsArchived })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Envelope", query.BudgetAccountId);
        if (account.IsArchived)
            throw new BudgetDomainException(Expense.ArchivedEnvelopeMessage);

        var statement = StatementParser.Parse(query.Content, budget.Currency);
        var keys = StatementSourceKey.ForAll(statement.Profile, caller.PersonId, statement.Transactions);
        var rows = statement.Transactions.Select((t, i) => (Transaction: t, Key: keys[i], Skip: StatementCandidateRules.SkipReason(t))).ToList();
        var candidates = rows.Where(r => r.Skip is null).ToList();

        var scope = Expense.ImportScopeOf(account.Visibility, account.OwnerPersonId);
        var imported = await AlreadyImportedAsync(budget.Id, scope, [.. candidates.Select(c => c.Key)], ct);
        var nearby = await NearbyExpensesAsync(caller, [.. candidates.Select(c => c.Transaction)], ct);
        var rules = await CategoryRulesAsync(budget.Id, scope, [.. candidates.Select(c => StatementText.MerchantKey(c.Transaction)).OfType<string>().Distinct()], ct);

        return new StatementPreviewDto(
            statement.Profile, statement.PeriodFrom, statement.PeriodTo, query.BudgetAccountId,
            [.. candidates.Select(c => ToCandidate(c.Transaction, c.Key, imported, nearby, rules, caller))],
            [.. rows.Where(r => r.Skip is not null).Select(r => new StatementSkippedRowDto(
                r.Transaction.LineNumber, r.Transaction.TransactionDate, F(r.Transaction.Amount), r.Transaction.Currency,
                StatementText.ExpenseDescription(r.Transaction), $"{r.Skip}"))],
            [.. statement.Issues.Select(i => new StatementIssueDto(i.LineNumber, i.Code.ToString()))]);
    }

    /// <summary>Keys already imported in this privacy scope — voided expenses included, so they do not come back (§5.2).</summary>
    private async Task<HashSet<string>> AlreadyImportedAsync(BudgetId budgetId, string scope, string[] keys, CancellationToken ct)
        => keys.Length == 0
            ? []
            : [.. await db.Expenses.AsNoTracking()
                .Where(e => e.BudgetId == budgetId && e.ImportScope == scope && keys.Contains(e.ImportSourceKey!))
                .Select(e => e.ImportSourceKey!)
                .ToListAsync(ct)];

    /// <summary>Categories learned for these merchants in this privacy scope (§5.1); exact match only.</summary>
    private async Task<Dictionary<string, ExpenseCategory>> CategoryRulesAsync(BudgetId budgetId, string scope, string[] merchantKeys, CancellationToken ct)
        => merchantKeys.Length == 0
            ? []
            : await db.CategoryRules.AsNoTracking()
                .Where(r => r.BudgetId == budgetId && r.ImportScope == scope && merchantKeys.Contains(r.MerchantKey))
                .ToDictionaryAsync(r => r.MerchantKey, r => r.Category, ct);

    /// <summary>
    /// Visible non-void expenses that could be the same purchase as a candidate: exact amount, date within
    /// two days. Category is deliberately not compared — a candidate's category is only a suggestion (§4.3).
    /// </summary>
    private async Task<List<ExpenseRow>> NearbyExpensesAsync(BudgetCaller caller, StatementTransaction[] candidates, CancellationToken ct)
    {
        if (candidates.Length == 0)
            return [];

        decimal[] amounts = [.. candidates.Select(c => -c.Amount).Distinct()];
        var from = candidates.Min(c => c.TransactionDate).AddDays(-DuplicateWindowDays);
        var to = candidates.Max(c => c.TransactionDate).AddDays(DuplicateWindowDays);

        return await db.VisibleTo(caller)
            .Where(e => !e.IsVoided && amounts.Contains(e.Amount) && e.OccurredOn >= from && e.OccurredOn <= to)
            .OrderBy(e => e.OccurredOn).ThenBy(e => e.Id)
            .Select(ExpenseReadExtensions.ToRow)
            .ToListAsync(ct);
    }

    private static StatementCandidateDto ToCandidate(
        StatementTransaction t, string key, HashSet<string> imported, List<ExpenseRow> nearby,
        Dictionary<string, ExpenseCategory> rules, BudgetCaller caller)
    {
        var amount = -t.Amount;
        List<ExpenseRow> matches = imported.Contains(key)
            ? []
            : [.. nearby.Where(e => e.Amount == amount && Math.Abs(e.OccurredOn.DayNumber - t.TransactionDate.DayNumber) <= DuplicateWindowDays).Take(MaxMatches)];
        var merchant = StatementText.MerchantKey(t);
        var status = "New";
        if (imported.Contains(key))
            status = "AlreadyImported";
        else if (matches.Count > 0)
            status = "PossibleDuplicate";

        return new StatementCandidateDto(
            t.LineNumber, key, t.TransactionDate, F(amount), StatementText.ExpenseDescription(t), merchant,
            merchant is not null && rules.TryGetValue(merchant, out var category) ? category.ToString() : null, status,
            [.. matches.Select(m => m.ToDto(caller))]);
    }

    private static string F(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
