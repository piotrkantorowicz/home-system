namespace Budget.Application.Queries.GetSettlement;

using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.Services;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetSettlementQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetSettlementQuery, SettlementDto>
{
    public async Task<SettlementDto> HandleAsync(GetSettlementQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAdultAsync(query.AuthSubject, ct);

        var currency = await db.Budgets.AsNoTracking()
            .Where(b => b.HouseholdId == caller.HouseholdId)
            .Select(b => (BudgetCurrency?)b.Currency)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Budget", caller.HouseholdId);

        // Only shared envelopes carry debt; personal expenses have a payer but no shares and must not count.
        var sharedAccountIds = db.BudgetAccounts
            .Where(a => db.Budgets.Any(b => b.Id == a.BudgetId && b.HouseholdId == caller.HouseholdId)
                && a.Visibility == AccountVisibility.Household)
            .Select(a => a.Id);
        var ledger = db.Expenses.AsNoTracking()
            .Where(e => sharedAccountIds.Contains(e.BudgetAccountId) && !e.IsVoided && e.FundingSource == FundingSource.Individual);

        var payments = await ledger
            .GroupBy(e => e.PaidByPersonId!.Value)
            .Select(g => new { Person = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(ct);
        var shares = await ledger
            .SelectMany(e => e.Shares)
            .GroupBy(s => s.PersonId)
            .Select(g => new { Person = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(ct);

        // Latest stored name per person, by recording time then ID, for people no longer on the roster.
        var payerNames = await ledger
            .Select(e => new { Person = e.PaidByPersonId!.Value, Name = e.PaidByDisplayName!, e.CreatedAt, e.Id })
            .ToListAsync(ct);
        var shareNames = await ledger
            .SelectMany(e => e.Shares.Select(s => new { Person = s.PersonId, Name = s.PersonDisplayName, e.CreatedAt, e.Id }))
            .ToListAsync(ct);
        var storedNames = payerNames.Concat(shareNames)
            .GroupBy(x => x.Person)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id.Value).First().Name);

        var net = SettlementCalculator.Net(
            payments.Select(p => (p.Person, Minor: ToMinor(p.Total))),
            shares.Select(s => (s.Person, Minor: ToMinor(s.Total))),
            []);

        string Name(Guid person)
            => caller.Members.FirstOrDefault(m => m.PersonId == person)?.DisplayName ?? storedNames.GetValueOrDefault(person, string.Empty);
        bool Former(Guid person) => caller.FindAdult(person) is null;

        var balances = net
            .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
            .Select(kv => new SettlementBalanceDto(kv.Key, Name(kv.Key), Former(kv.Key), Format(kv.Value)))
            .ToList();
        var suggestions = SettlementCalculator.Suggest(net)
            .Select(t => new SettlementTransferDto(t.From, Name(t.From), t.To, Name(t.To), Format(t.MinorUnits)))
            .ToList();

        return new SettlementDto(currency.ToString()!, net.Values.All(v => v == 0), balances, suggestions);
    }

    private static long ToMinor(decimal amount) => decimal.ToInt64(amount * 100m);

    private static string Format(long minor) => (minor / 100m).ToString("F2", CultureInfo.InvariantCulture);
}
