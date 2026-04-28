namespace Notifications.Application.Commands.UpdateChannelPreferences;

using Shared.Abstractions.Cqrs;

internal sealed class UpdateChannelPreferencesCommandValidator
    : ICommandValidator<UpdateChannelPreferencesCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateChannelPreferencesCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");
    }
}
