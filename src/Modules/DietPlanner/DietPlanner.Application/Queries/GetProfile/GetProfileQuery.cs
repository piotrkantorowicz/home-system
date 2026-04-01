namespace DietPlanner.Application.Queries.GetProfile;

using Shared.Abstractions.CQRS;

public sealed record GetProfileQuery(string UserId) : IQuery<UserProfileDto?>;
