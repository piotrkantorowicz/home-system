namespace DietPlanner.Api.Mcp;

using System.Security.Claims;
using ModelContextProtocol;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Web;

/// <summary>
/// The authenticated caller of a tool, whether the call came over <c>/mcp</c> or in-process through
/// <see cref="McpToolInvoker"/>. Tool arguments never name the caller.
/// </summary>
/// <param name="user">The caller bound to this tool call.</param>
internal sealed class McpCaller(McpToolUser user)
{
    public ClaimsPrincipal User()
        => user.Principal ?? throw new McpException("The request is not authenticated.");

    public string AuthSubject()
        => PersonalDataClaims.GetAuthSubject(User());

    public Guid PersonId()
    {
        try
        {
            return PersonalDataClaims.GetPersonId(User());
        }
        catch (ForbiddenException ex)
        {
            // Actionable for the agent: the person has not finished signing up in the app.
            throw new McpException(ex.Message);
        }
    }
}
