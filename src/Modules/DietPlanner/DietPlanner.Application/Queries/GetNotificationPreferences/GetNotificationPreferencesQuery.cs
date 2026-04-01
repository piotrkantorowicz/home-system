namespace DietPlanner.Application.Queries.GetNotificationPreferences;

using Shared.Abstractions.CQRS;

public sealed record GetNotificationPreferencesQuery(string UserId) : IQuery<NotificationPreferencesDto?>;
