namespace DietPlanner.Application.Commands.UpdateProfile;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken ct = default)
    {
        Gender? gender = command.Gender is not null
            ? Enum.Parse<Gender>(command.Gender, ignoreCase: true)
            : null;

        ActivityLevel? activityLevel = command.ActivityLevel is not null
            ? Enum.Parse<ActivityLevel>(command.ActivityLevel, ignoreCase: true)
            : null;

        var profile = await _repository.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);

        if (profile.UserId != command.UserId)
            throw new DietPlannerDomainException("You can only update your own profile.");

        profile.Update(
            command.DateOfBirth, gender,
            command.HeightCm, command.CurrentWeightKg, command.TargetWeightKg, activityLevel);

        _repository.Update(profile);
        await _unitOfWork.CommitAsync(ct);
    }
}
