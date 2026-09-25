namespace DietPlanner.Application.Commands.UpdateProfile;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateProfileCommandHandler(
    IUserProfileRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateProfileCommand>
{
    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        Gender? gender = command.Gender is not null
            ? Enum.Parse<Gender>(command.Gender, ignoreCase: true)
            : null;

        ActivityLevel? activityLevel = command.ActivityLevel is not null
            ? Enum.Parse<ActivityLevel>(command.ActivityLevel, ignoreCase: true)
            : null;

        var profile = await repository.GetByPersonIdAsync(command.PersonId, ct)
            ?? throw new NotFoundException("UserProfile", command.PersonId);

        if (profile.PersonId != command.PersonId)
            throw new DietPlannerDomainException("You can only update your own profile.");

        profile.Update(
            command.DateOfBirth, gender,
            command.HeightCm, command.CurrentWeightKg, command.TargetWeightKg, activityLevel, now);

        repository.Update(profile);
        await unitOfWork.CommitAsync(ct);
    }
}
