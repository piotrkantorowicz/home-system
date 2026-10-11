namespace Investments.Application.Common;

using System.Globalization;
using Investments.Application.Persistence;
using Investments.Application.Queries.GetWallet;
using Investments.Domain.Aggregates;
using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// The one place wallet visibility is applied to queries — before filtering, counting and lookup —
/// and the one wallet projection.
/// </summary>
internal static class WalletReadExtensions
{
    /// <summary>Wallets in the caller's household that the caller may see (same rule as <see cref="InvestmentsCaller.CanAccess"/>).</summary>
    public static IQueryable<Wallet> VisibleTo(this IQueryable<Wallet> wallets, InvestmentsCaller caller)
    {
        var householdId = caller.HouseholdId;
        var managedOwners = caller.ManagedOwnerIds;
        var members = caller.MemberIds;
        var seesShared = caller.IsAdult;

        return wallets.Where(w => w.HouseholdId == householdId
            && (managedOwners.Contains(w.OwnerPersonId)
                || (seesShared && w.Visibility == WalletVisibility.Shared && members.Contains(w.OwnerPersonId))));
    }

    /// <summary>Projects wallets with their latest recorded balance.</summary>
    public static IQueryable<WalletRow> ToRows(this IQueryable<Wallet> wallets, IQueryable<BalanceSnapshot> snapshots)
        => wallets.Select(w => new WalletRow(
            w.Id.Value, w.Name, w.Institution, w.Kind, w.Currency, w.Visibility, w.OwnerPersonId, w.OwnerName, w.IsArchived, w.Version, w.CreatedAt,
            snapshots.Where(s => s.WalletId == w.Id).OrderByDescending(s => s.AsOf).Select(s => (decimal?)s.Amount).FirstOrDefault(),
            snapshots.Where(s => s.WalletId == w.Id).OrderByDescending(s => s.AsOf).Select(s => (DateOnly?)s.AsOf).FirstOrDefault()));

    /// <summary>Reads one visible wallet as a DTO; not-found when it is missing or invisible.</summary>
    public static async Task<WalletDto> ReadWalletAsync(
        this IInvestmentsReadDbContext db, InvestmentsCaller caller, Guid id, CancellationToken ct)
    {
        var walletId = WalletId.From(id);
        var row = await db.Wallets.AsNoTracking()
            .Where(w => w.Id == walletId)
            .VisibleTo(caller)
            .ToRows(db.BalanceSnapshots)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Wallet", id);

        return row.ToDto(caller, await db.LoadRatesAsync(row.LatestValue(), ct));
    }

    /// <summary>The (currency, date) pairs a set of rows needs rates for.</summary>
    public static IEnumerable<(WalletCurrency, DateOnly)> LatestValue(this WalletRow r)
        => r.LatestAsOf is { } asOf ? [(r.Currency, asOf)] : [];

    public static WalletDto ToDto(this WalletRow r, InvestmentsCaller caller, FxRateBook rates)
    {
        var pln = r.LatestBalance is { } balance && r.LatestAsOf is { } asOf ? rates.ToPln(balance, r.Currency, asOf) : null;

        return new(
            r.Id, r.Name, r.Institution.ToString(), r.Kind.ToString(), r.Currency.ToString(), r.Visibility.ToString(),
            r.OwnerPersonId, caller.NameOf(r.OwnerPersonId) ?? r.OwnerName, r.IsArchived, r.Version, r.CreatedAt,
            caller.CanChangeVisibility(r.OwnerPersonId),
            Format(r.LatestBalance), r.LatestAsOf, Format(pln?.Amount), FxRateDto.From(pln?.Rate), pln?.RateMissing ?? false);
    }

    public static string? Format(decimal? amount) => amount?.ToString("F2", CultureInfo.InvariantCulture);
}
