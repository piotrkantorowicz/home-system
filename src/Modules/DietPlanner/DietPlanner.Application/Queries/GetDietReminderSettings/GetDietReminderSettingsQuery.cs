namespace DietPlanner.Application.Queries.GetDietReminderSettings;

using Shared.Abstractions.Cqrs;

public sealed record GetDietReminderSettingsQuery(string UserId) : IQuery<DietReminderSettingsDto?>;
