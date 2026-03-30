namespace DietPlanner.Application.Commands.CreateGoal;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class CreateGoalCommandHandler : ICommandHandler<CreateGoalCommand, Guid>
{
    private readonly IUserGoalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGoalCommandHandler(IUserGoalRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<Guid> HandleAsync(CreateGoalCommand command, CancellationToken ct = default)
    {
        var id = UserGoalId.New();
        var goal = UserGoal.Create(id, command.UserId, command.DailyCalorieTarget,
            command.ProteinGrams, command.CarbsGrams, command.FatGrams, command.FiberGrams);

        await _repository.AddAsync(goal, ct);
        await _unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
