namespace Investments.Application.Queries.GetNetWorth;

using System.Globalization;
using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Domain.Fx;
using Shared.Abstractions.Cqrs;

internal sealed class GetNetWorthQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db, TimeProvider clock)
    : IQueryHandler<GetNetWorthQuery, NetWorthDto>
{
    public async Task<NetWorthDto> HandleAsync(GetNetWorthQuery query, CancellationToken ct = default)
    {
        var scope = NetWorthScope.Parse(query.Scope, nameof(GetNetWorthQuery));
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var today = HouseholdCalendar.Today(clock);
        var asOf = query.AsOf ?? today;
        if (asOf > today)
            throw new CommandValidationException(
                nameof(GetNetWorthQuery), [new ValidationError("AsOf", "Net worth can't be computed for a future date.")]);

        // The current figure uses today's rates (owner decision); the history chart keeps month-end rates.
        var (calculator, wallets) = await db.LoadNetWorthAsync(caller, scope, asOf, asOf, ct, latestRates: true);
        var point = calculator.ValueAtLatestRates(asOf);
        var byId = wallets.ToDictionary(w => w.Id);
        string OwnerName(Guid personId) => caller.NameOf(personId) ?? wallets.First(w => w.OwnerPersonId == personId).OwnerName;

        return new NetWorthDto(
            asOf,
            scope,
            WalletReadExtensions.Format(point.Total),
            point.Incomplete,
            [.. point.Wallets.GroupBy(v => v.Wallet.Kind)
                .Select(g => new { g.Key, Total = g.Sum(v => v.Pln?.Amount ?? 0m) })
                .OrderByDescending(k => k.Total)
                .Select(k => new NetWorthKindDto(k.Key.ToString(), WalletReadExtensions.Format(k.Total)))],
            [.. point.Wallets.GroupBy(v => v.Wallet.OwnerPersonId)
                .Select(g => new { g.Key, Total = g.Sum(v => v.Pln?.Amount ?? 0m) })
                .OrderByDescending(o => o.Total)
                .Select(o => new NetWorthOwnerDto(o.Key, OwnerName(o.Key), WalletReadExtensions.Format(o.Total)))],
            [.. point.Wallets.Select(v => new NetWorthWalletDto(
                v.Wallet.Id.Value,
                byId[v.Wallet.Id].Name,
                v.Wallet.Kind.ToString(),
                v.Wallet.Currency.ToString(),
                WalletReadExtensions.Format(v.Balance?.Amount),
                v.Balance?.AsOf,
                WalletReadExtensions.Format(v.Pln?.Amount),
                v.Pln?.RateMissing ?? false))],
            [.. point.Wallets.Select(v => v.Pln?.Rate).OfType<FxQuote>().Distinct()
                .OrderBy(r => r.Currency)
                .Select(r => new NetWorthRateDto(r.Currency.ToString(), r.Mid.ToString("F6", CultureInfo.InvariantCulture), r.EffectiveDate, r.TableNo))]);
    }
}
