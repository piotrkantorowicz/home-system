namespace Investments.Application.Commands.UpdateWallet;

using Investments.Application.Common;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateWalletCommandValidator : ICommandValidator<UpdateWalletCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateWalletCommand command)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > Wallet.MaxNameLength)
            yield return new ValidationError(nameof(command.Name), $"Name must be 1–{Wallet.MaxNameLength} characters.");

        if (!EnumValue.TryParse<WalletVisibility>(command.Visibility, out _))
            yield return new ValidationError(nameof(command.Visibility), $"Visibility must be one of {EnumValue.Names<WalletVisibility>()}.");
    }
}
