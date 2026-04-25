namespace DietPlanner.Application.Commands.OverrideMealEntry;

using Shared.Abstractions.Cqrs;

internal sealed class OverrideMealEntryCommandValidator : ICommandValidator<OverrideMealEntryCommand>
{
    public IEnumerable<ValidationError> Validate(OverrideMealEntryCommand command)
    {
        if (command.Id == Guid.Empty)
            yield return new ValidationError(nameof(command.Id), "Id is required.");

        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.ActualRecipeId is null && command.ActualProducts.Count == 0)
            yield return new ValidationError(
                nameof(command.ActualProducts),
                "Override must include either a replacement recipe or at least one product.");

        if (command.ActualRecipeId == Guid.Empty)
            yield return new ValidationError(
                nameof(command.ActualRecipeId), "ActualRecipeId must not be empty.");

        for (var i = 0; i < command.ActualProducts.Count; i++)
        {
            ActualProductInput product = command.ActualProducts[i];

            if (product.ProductId == Guid.Empty)
                yield return new ValidationError(
                    $"ActualProducts[{i}].ProductId", "ProductId is required.");

            if (product.Amount <= 0)
                yield return new ValidationError(
                    $"ActualProducts[{i}].Amount", "Amount must be positive.");

            if (string.IsNullOrWhiteSpace(product.Unit))
                yield return new ValidationError(
                    $"ActualProducts[{i}].Unit", "Unit is required.");
        }
    }
}
