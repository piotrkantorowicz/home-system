namespace DietPlanner.Application.Commands.DeleteWeightEntry;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<DeleteWeightEntryCommand>
{
    public async Task HandleAsync(DeleteWeightEntryCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var entry = await weightEntryRepository.GetByIdAsync(WeightEntryId.From(command.EntryId), ct)
            ?? throw new NotFoundException("WeightEntry", command.EntryId);

        if (entry.UserId != command.UserId)
            throw new NotFoundException("WeightEntry", command.EntryId);

        var latest = await weightEntryRepository.GetLatestByUserAsync(command.UserId, entry.Id, ct);

        weightEntryRepository.Delete(entry);

        var profile = await userProfileRepository.GetByUserIdAsync(command.UserId, ct);
        if (profile is not null)
        {
            profile.UpdateCurrentWeight(latest?.WeightKg, now);
            userProfileRepository.Update(profile);
        }

        await unitOfWork.CommitAsync(ct);
    }
}
