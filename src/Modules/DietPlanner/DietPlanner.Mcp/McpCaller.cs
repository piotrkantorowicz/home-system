namespace DietPlanner.Mcp;

using System.Security.Claims;
using ModelContextProtocol;
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

    // Same claims and messages as REST's PersonalDataClaims; the claims pipeline sets them for both transports.
    public string AuthSubject()
        => User().FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User().FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

    // Actionable for the agent: the person has not finished signing up in the app.
    public Guid PersonId()
        => Guid.TryParse(User().FindFirstValue("person_id"), out Guid personId) && personId != Guid.Empty
            ? personId
            : throw new McpException("Sync your Person and join or create a household before using DietPlanner personal data.");
}
