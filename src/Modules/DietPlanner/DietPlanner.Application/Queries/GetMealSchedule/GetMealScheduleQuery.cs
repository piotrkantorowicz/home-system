namespace DietPlanner.Application.Queries.GetMealSchedule;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads a household member's meal schedule with its slots; <see langword="null"/> until one has been created.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
/// <param name="ForPersonId">Household member whose schedule to read; the caller when <see langword="null"/>.</param>
public sealed record GetMealScheduleQuery(
    Guid PersonId,
    string AuthSubject,
    Guid? ForPersonId = null) : IQuery<MealScheduleConfigDto?>;
