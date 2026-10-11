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

    /// <summary>Loads the household's wallets among <paramref name="ids"/> for modification; unknown ids are skipped.</summary>
    /// <param name="ids">The wallets.</param>
    /// <param name="householdId">The household they must belong to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<Wallet>> GetManyAsync(IReadOnlyCollection<WalletId> ids, Guid householdId, CancellationToken ct);
}
