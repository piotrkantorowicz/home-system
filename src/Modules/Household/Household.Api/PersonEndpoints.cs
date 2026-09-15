namespace Household.Api;

using System.Security.Claims;
using Household.Api.Identity;
using Household.Application.Commands.SyncCurrentPerson;
using Household.Application.Queries.GetCurrentPerson;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

internal static class PersonEndpoints
{
    internal static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/persons")
            .RequireAuthorization()
            .WithTags("Persons");

        group.MapPost("/me/sync", SyncCurrentPerson)
            .WithName("SyncCurrentPerson")
            .WithSummary("Ensure a Person exists for the signed-in account and refresh its profile");

        group.MapGet("/me", GetCurrentPerson)
            .WithName("GetCurrentPerson")
            .WithSummary("Get the Person record for the signed-in account");

        return app;
    }

    private static async Task<Ok<SyncCurrentPersonResponse>> SyncCurrentPerson(
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var authSubject = user.GetAuthSubject()
            ?? throw new UnauthorizedAccessException("Missing subject claim.");

        var personId = await dispatcher.SendAsync<SyncCurrentPersonCommand, Guid>(
            new SyncCurrentPersonCommand(
                authSubject,
                user.GetDisplayName(),
                user.GetEmail(),
                user.GetPicture()),
            ct);

        return TypedResults.Ok(new SyncCurrentPersonResponse(personId));
    }

    private static async Task<Results<Ok<CurrentPersonDto>, NotFound>> GetCurrentPerson(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var authSubject = user.GetAuthSubject()
            ?? throw new UnauthorizedAccessException("Missing subject claim.");

        var person = await dispatcher.SendAsync<GetCurrentPersonQuery, CurrentPersonDto?>(
            new GetCurrentPersonQuery(authSubject), ct);

        return person is null ? TypedResults.NotFound() : TypedResults.Ok(person);
    }
}

internal sealed record SyncCurrentPersonResponse(Guid PersonId);
