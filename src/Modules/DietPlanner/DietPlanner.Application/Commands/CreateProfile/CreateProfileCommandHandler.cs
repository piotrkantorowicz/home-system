namespace DietPlanner.Application.Commands.CreateProfile;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateProfileCommandHandler(
    IUserProfileRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateProfileCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProfileCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        Gender? gender = command.Gender is not null
            ? Enum.Parse<Gender>(command.Gender, ignoreCase: true)
            : null;

        ActivityLevel? activityLevel = command.ActivityLevel is not null
            ? Enum.Parse<ActivityLevel>(command.ActivityLevel, ignoreCase: true)
            : null;

        var id = UserProfileId.New();
        UserProfile profile = UserProfile.Create(
            id, command.PersonId, command.DateOfBirth, gender,
            command.HeightCm, command.CurrentWeightKg, command.TargetWeightKg, activityLevel, now);

        await repository.AddAsync(profile, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
