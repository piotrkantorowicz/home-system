namespace Investments.Application.Commands.CreateWallet;

using Investments.Application.Common;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class CreateWalletCommandValidator : ICommandValidator<CreateWalletCommand>
{
    public IEnumerable<ValidationError> Validate(CreateWalletCommand command)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > Wallet.MaxNameLength)
            yield return new ValidationError(nameof(command.Name), $"Name must be 1–{Wallet.MaxNameLength} characters.");

        if (!EnumValue.TryParse<WalletInstitution>(command.Institution, out _))
            yield return new ValidationError(nameof(command.Institution), $"Institution must be one of {EnumValue.Names<WalletInstitution>()}.");

        if (!EnumValue.TryParse<WalletKind>(command.Kind, out _))
            yield return new ValidationError(nameof(command.Kind), $"Kind must be one of {EnumValue.Names<WalletKind>()}.");

        if (!EnumValue.TryParse<WalletCurrency>(command.Currency, out _))
            yield return new ValidationError(nameof(command.Currency), $"Currency must be one of {EnumValue.Names<WalletCurrency>()}.");

        if (!EnumValue.TryParse<WalletVisibility>(command.Visibility, out _))
            yield return new ValidationError(nameof(command.Visibility), $"Visibility must be one of {EnumValue.Names<WalletVisibility>()}.");
    }
}
