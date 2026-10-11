namespace Investments.Domain.Abstractions;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;

/// <summary>Write-side access to the <c>BalanceSnapshot</c> aggregate. Queries bypass it.</summary>
public interface IBalanceSnapshotRepository
{
    /// <summary>Stages a new snapshot for the next commit.</summary>
    /// <param name="snapshot">The snapshot to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(BalanceSnapshot snapshot, CancellationToken ct);

    /// <summary>Loads the wallet's snapshot for a date, or <see langword="null"/> when there is none.</summary>
    /// <param name="walletId">The wallet.</param>
    /// <param name="asOf">The date.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BalanceSnapshot?> GetAsync(WalletId walletId, DateOnly asOf, CancellationToken ct);

    /// <summary>Stages a snapshot for deletion on the next commit.</summary>
    /// <param name="snapshot">The snapshot to remove.</param>
    void Remove(BalanceSnapshot snapshot);
}
