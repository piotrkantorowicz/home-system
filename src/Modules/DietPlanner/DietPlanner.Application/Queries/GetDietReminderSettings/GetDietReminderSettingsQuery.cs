namespace DietPlanner.Application.Queries.GetDietReminderSettings;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's notification preferences; <see langword="null"/> until they have been saved once.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetDietReminderSettingsQuery(string UserId) : IQuery<DietReminderSettingsDto?>;
