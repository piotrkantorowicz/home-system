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

        if (entry.PersonId != command.PersonId)
            throw new NotFoundException("WeightEntry", command.EntryId);

        var latest = await weightEntryRepository.GetLatestByPersonAsync(command.PersonId, entry.Id, ct);

        weightEntryRepository.Delete(entry);

        var profile = await userProfileRepository.GetByPersonIdAsync(command.PersonId, ct);
        if (profile is not null)
        {
            profile.UpdateCurrentWeight(latest?.WeightKg, now);
            userProfileRepository.Update(profile);
        }

        await unitOfWork.CommitAsync(ct);
    }
}
