using System.Security.Claims;
using Household.Api.Identity;
using Household.Application.Commands.AddExistingPersonAsMember;
using Household.Application.Commands.ChangeMemberRole;
using Household.Application.Commands.ConvertManagedMemberToAccount;
using Household.Application.Commands.CreateHousehold;
using Household.Application.Commands.CreateManagedMember;
using Household.Application.Commands.DeleteHousehold;
using Household.Application.Commands.InvitePersonByEmail;
using Household.Application.Commands.LeaveHousehold;
using Household.Application.Commands.RemoveMember;
using Household.Application.Commands.RenameHousehold;
using Household.Application.Commands.RevokeInvitation;
using Household.Application.Queries.GetMyHousehold;
using Household.Application.Queries.ListHouseholdMembers;
using Household.Application.Queries.ListPendingInvitations;
using Household.Application.Queries.ListPickablePersons;
using Household.Application.Queries.Projections;
using Household.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

namespace Household.Api;

internal static class HouseholdEndpoints
{
    internal static IEndpointRouteBuilder MapHouseholdEndpointsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/households")
            .RequireAuthorization()
            .WithTags("Households");

        group.MapPost("/", Create).WithName("CreateHousehold");
        group.MapGet("/me", GetMine).WithName("GetMyHousehold");
        group.MapGet("/pickable-persons", ListPickablePersons).WithName("ListPickablePersons");
        group.MapPut("/{id:guid}", Rename).WithName("RenameHousehold");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteHousehold");
        group.MapPost("/{id:guid}/leave", Leave).WithName("LeaveHousehold");
        group.MapGet("/{id:guid}/members", ListMembers).WithName("ListHouseholdMembers");
        group.MapPost("/{id:guid}/members", AddMember).WithName("AddHouseholdMember");
        group.MapPost("/{id:guid}/managed-members", AddManagedMember).WithName("CreateManagedMember");
        group.MapDelete("/{id:guid}/members/{personId:guid}", RemoveMember).WithName("RemoveHouseholdMember");
        group.MapPut("/{id:guid}/members/{personId:guid}/role", ChangeRole).WithName("ChangeHouseholdMemberRole");
        group.MapPost("/{id:guid}/members/{personId:guid}/convert-to-account", ConvertToAccount)
            .WithName("ConvertManagedMemberToAccount");
        group.MapGet("/{id:guid}/invitations", ListInvitations).WithName("ListPendingInvitations");
        group.MapPost("/{id:guid}/invitations", Invite).WithName("InvitePersonByEmail");
        group.MapDelete("/{id:guid}/invitations/{invitationId:guid}", RevokeInvitation).WithName("RevokeInvitation");

        return app;
    }

    private static async Task<Created> Create(
        CreateHouseholdRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var id = await dispatcher.SendAsync<CreateHouseholdCommand, Guid>(
            new CreateHouseholdCommand(Sub(user), request.Name), ct);
        return TypedResults.Created($"/api/households/{id}");
    }

    private static async Task<Results<Ok<MyHouseholdDto>, NotFound>> GetMine(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<GetMyHouseholdQuery, MyHouseholdDto?>(
            new GetMyHouseholdQuery(Sub(user)), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyList<PickablePersonDto>>> ListPickablePersons(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<ListPickablePersonsQuery, IReadOnlyList<PickablePersonDto>>(
            new ListPickablePersonsQuery(Sub(user)), ct));

    private static async Task<NoContent> Rename(
        Guid id, RenameHouseholdRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new RenameHouseholdCommand(Sub(user), id, request.Name), ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Delete(
        Guid id, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new DeleteHouseholdCommand(Sub(user), id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Leave(
        Guid id, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new LeaveHouseholdCommand(Sub(user), id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<HouseholdMemberDto>>> ListMembers(
        Guid id, ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<ListHouseholdMembersQuery, IReadOnlyList<HouseholdMemberDto>>(
            new ListHouseholdMembersQuery(Sub(user), id), ct));

    private static async Task<NoContent> AddMember(
        Guid id, AddMemberRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new AddExistingPersonAsMemberCommand(
            Sub(user), id, request.PersonId, ParseRole(request.Role), request.Nickname), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Created> AddManagedMember(
        Guid id, CreateManagedMemberRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var personId = await dispatcher.SendAsync<CreateManagedMemberCommand, Guid>(
            new CreateManagedMemberCommand(
                Sub(user), id, request.DisplayName, request.Email, ParseRole(request.Role), request.Nickname),
            ct);
        return TypedResults.Created($"/api/households/{id}/members/{personId}");
    }

    private static async Task<NoContent> RemoveMember(
        Guid id, Guid personId, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new RemoveMemberCommand(Sub(user), id, personId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ChangeRole(
        Guid id, Guid personId, ChangeRoleRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new ChangeMemberRoleCommand(
            Sub(user), id, personId, ParseRole(request.Role)), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<InvitationDto>>> ListInvitations(
        Guid id, ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<ListPendingInvitationsQuery, IReadOnlyList<InvitationDto>>(
            new ListPendingInvitationsQuery(Sub(user), id), ct));

    private static async Task<Ok<InvitePersonByEmailResult>> Invite(
        Guid id, InviteRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<InvitePersonByEmailCommand, InvitePersonByEmailResult>(
            new InvitePersonByEmailCommand(Sub(user), id, request.Email, ParseRole(request.Role)), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> RevokeInvitation(
        Guid id, Guid invitationId, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new RevokeInvitationCommand(Sub(user), id, invitationId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ConvertToAccount(
        Guid id, Guid personId, ConvertToAccountRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(
            new ConvertManagedMemberToAccountCommand(Sub(user), id, personId, request.Email), ct);
        return TypedResults.NoContent();
    }

    private static string Sub(ClaimsPrincipal user)
        => user.GetAuthSubject() ?? throw new UnauthorizedAccessException("Missing subject claim.");

    private static HouseholdRole ParseRole(string role)
        => Enum.TryParse<HouseholdRole>(role, ignoreCase: true, out var parsed)
           && Enum.IsDefined(parsed)
            ? parsed
            : throw new CommandValidationException(
                "Role", [new ValidationError("Role", $"'{role}' is not a valid role.")]);
}

internal sealed record CreateHouseholdRequest(string Name);
internal sealed record RenameHouseholdRequest(string Name);
internal sealed record AddMemberRequest(Guid PersonId, string Role, string? Nickname);
internal sealed record CreateManagedMemberRequest(string DisplayName, string? Email, string Role, string? Nickname);
internal sealed record ChangeRoleRequest(string Role);
internal sealed record InviteRequest(string Email, string Role);
internal sealed record ConvertToAccountRequest(string Email);
