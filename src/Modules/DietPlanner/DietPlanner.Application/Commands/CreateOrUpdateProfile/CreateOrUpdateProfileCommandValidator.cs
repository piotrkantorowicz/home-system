namespace DietPlanner.Application.Commands.CreateOrUpdateProfile;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;

internal sealed class CreateOrUpdateProfileCommandValidator : ICommandValidator<CreateOrUpdateProfileCommand>
{
    public IEnumerable<ValidationError> Validate(CreateOrUpdateProfileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.Gender is not null && !Enum.TryParse<Gender>(command.Gender, ignoreCase: true, out _))
            yield return new ValidationError(nameof(command.Gender), "Gender must be a valid value (Male, Female, Other).");

        if (command.ActivityLevel is not null && !Enum.TryParse<ActivityLevel>(command.ActivityLevel, ignoreCase: true, out _))
            yield return new ValidationError(nameof(command.ActivityLevel), "ActivityLevel must be a valid value (Sedentary, LightlyActive, ModeratelyActive, VeryActive, ExtraActive).");

        if (command.HeightCm is <= 0)
            yield return new ValidationError(nameof(command.HeightCm), "HeightCm must be positive.");

        if (command.CurrentWeightKg is <= 0)
            yield return new ValidationError(nameof(command.CurrentWeightKg), "CurrentWeightKg must be positive.");

        if (command.TargetWeightKg is <= 0)
            yield return new ValidationError(nameof(command.TargetWeightKg), "TargetWeightKg must be positive.");
    }
}
