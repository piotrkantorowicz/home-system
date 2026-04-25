namespace DietPlanner.Application.Queries.GetNotificationPreferences;

using Shared.Abstractions.Cqrs;

public sealed record GetNotificationPreferencesQuery(string UserId) : IQuery<NotificationPreferencesDto?>;
