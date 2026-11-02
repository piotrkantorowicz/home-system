namespace Investments.Application.Commands.DeleteContribution;

using Shared.Abstractions.Cqrs;

/// <summary>Removes one deposit or withdrawal. Allowed on archived wallets.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="WalletId">A wallet the caller may see.</param>
/// <param name="ContributionId">One of that wallet's contributions; not-found otherwise.</param>
public sealed record DeleteContributionCommand(string AuthSubject, Guid WalletId, Guid ContributionId) : ICommand;
