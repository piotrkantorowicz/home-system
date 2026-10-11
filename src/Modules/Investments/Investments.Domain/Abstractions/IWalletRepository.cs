namespace Investments.Domain.Abstractions;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;

/// <summary>Write-side access to the <c>Wallet</c> aggregate. Queries bypass it.</summary>
public interface IWalletRepository
{
    /// <summary>Stages a new wallet for the next commit.</summary>
    /// <param name="wallet">The wallet to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(Wallet wallet, CancellationToken ct);

    /// <summary>Loads a wallet for modification, or <see langword="null"/> when it is not in the household.</summary>
    /// <param name="id">The wallet.</param>
    /// <param name="householdId">The household it must belong to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Wallet?> GetAsync(WalletId id, Guid householdId, CancellationToken ct);
}
