namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateProfile;
using DietPlanner.Application.Commands.UpdateProfile;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.Application.Queries.GetWeightPrediction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoints for the caller's body profile and weight prediction (<c>/api/v1/profile</c>).
/// </summary>
public static class ProfileEndpoints
{
    /// <summary>Maps profile create, read, update and the prediction query; all require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
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

        group.MapGet("/prediction", GetWeightPrediction)
            .WithName("GetWeightPrediction")
            .WithSummary("Get weight loss/gain prediction for a given calorie target")
            .WithDescription(
                "Calculates BMR, TDEE, weekly weight change and estimated goal date based on the user's biometrics profile and the supplied daily calorie target. Returns 404 if the profile does not exist or is missing required biometric fields.")
            .Produces<WeightPredictionDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateProfile)
            .WithName("CreateProfile")
            .WithSummary("Create a biometrics profile for the current user")
            .WithDescription("Creates a new biometrics profile. All biometric fields are optional.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/", UpdateProfile)
            .WithName("UpdateProfile")
            .WithSummary("Update the current user's biometrics profile")
            .WithDescription("Replaces all biometric fields on the existing profile. Pass null to clear a field.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
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

    private static async Task<IResult> GetWeightPrediction(
        decimal dailyCalorieTarget,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        WeightPredictionDto? result = await dispatcher.SendAsync<GetWeightPredictionQuery, WeightPredictionDto?>(
            new GetWeightPredictionQuery(userId, dailyCalorieTarget), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateProfile(
        ProfileRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        Guid id = await dispatcher.SendAsync<CreateProfileCommand, Guid>(
            new CreateProfileCommand(
                userId, request.DateOfBirth, request.Gender, request.HeightCm,
                request.CurrentWeightKg, request.TargetWeightKg, request.ActivityLevel), ct);
        return TypedResults.Created($"/api/v1/profile/{id}");
    }

    private static async Task<IResult> UpdateProfile(
        ProfileRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateProfileCommand(
                userId, request.DateOfBirth, request.Gender, request.HeightCm,
                request.CurrentWeightKg, request.TargetWeightKg, request.ActivityLevel), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

/// <summary>
/// Body of profile create and update; every field is optional.
/// </summary>
/// <param name="DateOfBirth">Date of birth, used to derive age.</param>
/// <param name="Gender"><c>Male</c>, <c>Female</c> or <c>Other</c>.</param>
/// <param name="HeightCm">Height in centimetres.</param>
/// <param name="CurrentWeightKg">Current weight in kilograms.</param>
/// <param name="TargetWeightKg">Target weight in kilograms.</param>
/// <param name="ActivityLevel">One of <c>Sedentary</c>, <c>LightlyActive</c>, <c>ModeratelyActive</c>, <c>VeryActive</c>, <c>ExtraActive</c>.</param>
public sealed record ProfileRequest(
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel);
