namespace Investments.Application.Commands.RecordCheckIn;

using Investments.Application.Common;
using Investments.Application.Fx;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordCheckInCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IBalanceSnapshotRepository snapshots,
    IInvestmentsUnitOfWork unitOfWork,
    FxRateSync fx,
    TimeProvider clock) : ICommandHandler<RecordCheckInCommand, RecordCheckInResult>
{
    public async Task<RecordCheckInResult> HandleAsync(RecordCheckInCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var now = clock.GetUtcNow();
        var today = HouseholdCalendar.Today(clock);
        WalletId[] ids = [.. command.Entries.Select(e => WalletId.From(e.WalletId))];

        // Invisible and unknown wallets look the same: "not found" for the id the caller sent.
        var visible = (await wallets.GetManyAsync(ids, caller.HouseholdId, ct))
            .Where(caller.CanAccess)
            .ToDictionary(w => w.Id);
        var existing = (await snapshots.GetForDateAsync([.. visible.Keys], command.AsOf, ct))
            .ToDictionary(s => s.WalletId);

        // Validate every entry before writing any; staged changes are discarded on failure.
        Dictionary<string, string[]> failures = [];
        var created = 0;
        foreach (var entry in command.Entries)
        {
            var id = WalletId.From(entry.WalletId);
            if (!visible.TryGetValue(id, out var wallet))
            {
                failures[entry.WalletId.ToString()] = ["wallet not found"];
                continue;
            }
            if (!Money.TryParse(entry.Amount, out var amount))
            {
                failures[entry.WalletId.ToString()] = ["amount must be a decimal such as 12450.10, not negative, at most two decimals"];
                continue;
            }

            try
            {
                if (existing.TryGetValue(id, out var snapshot))
                {
                    snapshot.Correct(amount, entry.Note, now);
                }
                else
                {
                    await snapshots.AddAsync(BalanceSnapshot.Record(
                        BalanceSnapshotId.New(), wallet, command.AsOf, amount, entry.Note,
                        caller.PersonId, caller.NameOf(caller.PersonId) ?? "Unknown", today, now), ct);
                    created++;
                }
            }
            catch (InvestmentsDomainException ex)
            {
                failures[entry.WalletId.ToString()] = [ex.Message];
            }
        }

        if (failures.Count > 0)
            throw new InvestmentsDomainException(
                $"Nothing was saved. Fix these balances: {string.Join("; ", failures.Select(f => $"{f.Key}: {f.Value[0]}"))}", failures);

        await unitOfWork.CommitOrThrowConflictAsync(ct);
        await BackfillRatesAsync(visible.Values.Select(w => w.Currency), today, ct);

        return new RecordCheckInResult(command.AsOf, command.Entries.Count, created);
    }

    private async Task BackfillRatesAsync(IEnumerable<WalletCurrency> currencies, DateOnly today, CancellationToken ct)
    {
        // Same rule as a single balance: a sync failure never fails the committed write.
        foreach (var currency in currencies.Where(c => c != WalletCurrency.PLN).Distinct())
            await fx.TrySyncAsync(currency, today, ct);
    }
}
