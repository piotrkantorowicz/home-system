namespace DietPlanner.Application.Commands.LogWeightEntry;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class LogWeightEntryCommandHandler
    : ICommandHandler<LogWeightEntryCommand, LogWeightEntryResult>
{
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogWeightEntryCommandHandler(
        IWeightEntryRepository weightEntryRepository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork)
    {
        _weightEntryRepository = weightEntryRepository;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LogWeightEntryResult> HandleAsync(
        LogWeightEntryCommand command, CancellationToken ct = default)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);

        var existing = await _weightEntryRepository.GetByUserAndDateAsync(
            command.UserId, command.Date, ct);

        bool created;
        WeightEntry entry;
        if (existing is null)
        {
            entry = WeightEntry.Create(WeightEntryId.New(), command.UserId, command.Date, command.WeightKg);
            await _weightEntryRepository.AddAsync(entry, ct);
            created = true;
        }
        else
        {
            existing.ChangeWeight(command.WeightKg);
            entry = existing;
            created = false;
        }

        profile.UpdateCurrentWeight(command.WeightKg);
        _userProfileRepository.Update(profile);

        await _unitOfWork.CommitAsync(ct);

        return new LogWeightEntryResult(entry.Id.Value, created);
    }
}
