namespace DietPlanner.Application.Commands.DeleteWeightEntry;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteWeightEntryCommandHandler : ICommandHandler<DeleteWeightEntryCommand>
{
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWeightEntryCommandHandler(
        IWeightEntryRepository weightEntryRepository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork)
    {
        _weightEntryRepository = weightEntryRepository;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteWeightEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _weightEntryRepository.GetByIdAsync(WeightEntryId.From(command.EntryId), ct)
            ?? throw new NotFoundException("WeightEntry", command.EntryId);

        if (entry.UserId != command.UserId)
            throw new NotFoundException("WeightEntry", command.EntryId);

        var latest = await _weightEntryRepository.GetLatestByUserAsync(command.UserId, entry.Id, ct);

        _weightEntryRepository.Delete(entry);

        var profile = await _userProfileRepository.GetByUserIdAsync(command.UserId, ct);
        if (profile is not null)
        {
            profile.UpdateCurrentWeight(latest?.WeightKg);
            _userProfileRepository.Update(profile);
        }

        await _unitOfWork.CommitAsync(ct);
    }
}
