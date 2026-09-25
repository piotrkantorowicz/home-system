namespace DietPlanner.Application.Commands.LogWeightEntry;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class LogWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<LogWeightEntryCommand, LogWeightEntryResult>
{
    public async Task<LogWeightEntryResult> HandleAsync(
        LogWeightEntryCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var profile = await userProfileRepository.GetByPersonIdAsync(command.PersonId, ct)
            ?? throw new NotFoundException("UserProfile", command.PersonId);

        var existing = await weightEntryRepository.GetByPersonAndDateAsync(
            command.PersonId, command.Date, ct);

        bool created;
        WeightEntry entry;
        if (existing is null)
        {
            entry = WeightEntry.Create(WeightEntryId.New(), command.PersonId, command.Date, command.WeightKg, now);
            await weightEntryRepository.AddAsync(entry, ct);
            created = true;
        }
        else
        {
            existing.ChangeWeight(command.WeightKg, now);
            entry = existing;
            created = false;
        }

        profile.UpdateCurrentWeight(command.WeightKg, now);
        userProfileRepository.Update(profile);

        await unitOfWork.CommitAsync(ct);

        return new LogWeightEntryResult(entry.Id.Value, created);
    }
}
