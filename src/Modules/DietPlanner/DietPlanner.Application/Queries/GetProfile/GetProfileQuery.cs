namespace DietPlanner.Application.Queries.GetProfile;

using Shared.Abstractions.Cqrs;

public sealed record GetProfileQuery(string UserId) : IQuery<UserProfileDto?>;
