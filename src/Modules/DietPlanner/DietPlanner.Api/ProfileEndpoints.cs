namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateOrUpdateProfile;
using DietPlanner.Application.Queries.GetProfile;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.CQRS;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", GetProfile)
            .WithName("GetProfile")
            .WithSummary("Get the current user's biometrics profile")
            .WithDescription("Returns the biometrics profile for the current user, or 404 if none exists.")
            .Produces<UserProfileDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/", CreateOrUpdateProfile)
            .WithName("CreateOrUpdateProfile")
            .WithSummary("Create or update the current user's biometrics profile")
            .WithDescription("Creates a new profile if none exists, or updates the existing one. All biometric fields are optional.")
            .Produces<Guid>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetProfile(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        UserProfileDto? result = await dispatcher.SendAsync<GetProfileQuery, UserProfileDto?>(
            new GetProfileQuery(userId), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateOrUpdateProfile(
        ProfileRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        Guid id = await dispatcher.SendAsync<CreateOrUpdateProfileCommand, Guid>(
            new CreateOrUpdateProfileCommand(
                userId, request.DateOfBirth, request.Gender, request.HeightCm,
                request.CurrentWeightKg, request.TargetWeightKg, request.ActivityLevel), ct);
        return TypedResults.Ok(id);
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record ProfileRequest(
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel);
