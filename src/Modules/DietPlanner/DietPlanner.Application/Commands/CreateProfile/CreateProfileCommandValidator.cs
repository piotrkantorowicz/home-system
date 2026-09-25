namespace DietPlanner.Application.Commands.CreateProfile;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class CreateProfileCommandValidator : ICommandValidator<CreateProfileCommand>
{
    public IEnumerable<ValidationError> Validate(CreateProfileCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

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
