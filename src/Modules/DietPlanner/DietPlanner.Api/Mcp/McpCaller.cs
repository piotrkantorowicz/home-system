namespace DietPlanner.Api.Mcp;

using System.Security.Claims;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// The authenticated caller of an MCP tool, read from the request the transport authenticated. Tool
/// arguments never name the caller.
/// </summary>
internal static class McpCaller
{
    public static ClaimsPrincipal User(RequestContext<CallToolRequestParams> context)
        => context.User ?? throw new McpException("The request is not authenticated.");

    public static string AuthSubject(RequestContext<CallToolRequestParams> context)
        => PersonalDataClaims.GetAuthSubject(User(context));

    public static Guid PersonId(RequestContext<CallToolRequestParams> context)
    {
        try
        {
            return PersonalDataClaims.GetPersonId(User(context));
        }
        catch (ForbiddenException ex)
        {
            // Actionable for the agent: the person has not finished signing up in the app.
            throw new McpException(ex.Message);
        }
    }
}
