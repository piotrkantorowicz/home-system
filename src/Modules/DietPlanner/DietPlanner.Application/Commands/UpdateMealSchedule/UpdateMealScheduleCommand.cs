namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using Shared.Abstractions.Cqrs;

/// <summary>
/// One desired slot in a schedule update.
/// </summary>
/// <param name="Id">Identifier of an existing slot to keep, or <see langword="null"/> for a new slot.</param>
/// <param name="Name">Display name; required.</param>
/// <param name="DefaultTime">Default time of day as <c>HH:mm</c>.</param>
public sealed record MealSlotInput(Guid? Id, string Name, string DefaultTime);
/// <summary>
/// Replaces the caller's meal schedule with the given slots (1–8), creating the schedule on first use. Slots omitted from the list are removed, which fails if any meal entry still references them.
/// </summary>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
/// <param name="Slots">The desired slots in display order.</param>
public sealed record UpdateMealScheduleCommand(string UserId, IReadOnlyList<MealSlotInput> Slots) : ICommand;
