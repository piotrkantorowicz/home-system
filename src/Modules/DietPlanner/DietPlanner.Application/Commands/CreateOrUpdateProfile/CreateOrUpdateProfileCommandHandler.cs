namespace DietPlanner.Application.Commands.CreateOrUpdateProfile;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class CreateOrUpdateProfileCommandHandler : ICommandHandler<CreateOrUpdateProfileCommand, Guid>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrUpdateProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<Guid> HandleAsync(CreateOrUpdateProfileCommand command, CancellationToken ct = default)
    {
        Gender? gender = command.Gender is not null
            ? Enum.Parse<Gender>(command.Gender, ignoreCase: true)
            : null;

        ActivityLevel? activityLevel = command.ActivityLevel is not null
            ? Enum.Parse<ActivityLevel>(command.ActivityLevel, ignoreCase: true)
            : null;

        UserProfile? existing = await _repository.GetByUserIdAsync(command.UserId, ct);

        if (existing is null)
        {
            var id = UserProfileId.New();
            var profile = UserProfile.Create(
                id, command.UserId, command.DateOfBirth, gender,
                command.HeightCm, command.CurrentWeightKg, command.TargetWeightKg, activityLevel);

            await _repository.AddAsync(profile, ct);
            await _unitOfWork.CommitAsync(ct);

            return id.Value;
        }

        existing.Update(
            command.DateOfBirth, gender,
            command.HeightCm, command.CurrentWeightKg, command.TargetWeightKg, activityLevel);

        _repository.Update(existing);
        await _unitOfWork.CommitAsync(ct);

        return existing.Id.Value;
    }
}
