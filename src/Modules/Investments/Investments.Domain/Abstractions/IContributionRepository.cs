namespace Investments.Domain.Abstractions;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;

/// <summary>Write-side access to the <c>Contribution</c> aggregate. Queries bypass it.</summary>
public interface IContributionRepository
{
    /// <summary>Stages a new contribution for the next commit.</summary>
    /// <param name="contribution">The contribution to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(Contribution contribution, CancellationToken ct);

    /// <summary>Loads one of the wallet's contributions, or <see langword="null"/> when it has no such one.</summary>
    /// <param name="walletId">The wallet.</param>
    /// <param name="id">The contribution.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Contribution?> GetAsync(WalletId walletId, ContributionId id, CancellationToken ct);

    /// <summary>Stages a contribution for deletion on the next commit.</summary>
    /// <param name="contribution">The contribution to remove.</param>
    void Remove(Contribution contribution);
}
