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

        var (calculator, wallets) = await db.LoadNetWorthAsync(caller, scope, asOf, asOf, ct);
        var point = calculator.ValueAt(asOf);
        var byId = wallets.ToDictionary(w => w.Id);
        string OwnerName(Guid personId) => caller.NameOf(personId) ?? wallets.First(w => w.OwnerPersonId == personId).OwnerName;

        return new NetWorthDto(
            asOf,
            scope,
            Format(point.Total),
            point.Incomplete,
            [.. point.Wallets.GroupBy(v => v.Wallet.Kind)
                .Select(g => new NetWorthKindDto(g.Key.ToString(), Format(g.Sum(v => v.Pln?.Amount ?? 0m))))
                .OrderByDescending(k => decimal.Parse(k.Total, CultureInfo.InvariantCulture))],
            [.. point.Wallets.GroupBy(v => v.Wallet.OwnerPersonId)
                .Select(g => new { g.Key, Total = g.Sum(v => v.Pln?.Amount ?? 0m) })
                .OrderByDescending(o => o.Total)
                .Select(o => new NetWorthOwnerDto(o.Key, OwnerName(o.Key), Format(o.Total)))],
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

    private static string Format(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}
