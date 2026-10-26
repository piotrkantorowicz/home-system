namespace Investments.Mcp;

using System.Security.Claims;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
/// The authenticated caller of an MCP tool, read from the request the transport authenticated. Tool
/// arguments never name the caller.
/// </summary>
internal static class McpCaller
{
    // ponytail: mirrors DietPlanner's McpCaller; #643 replaces both with one transport-free caller.
    public static string AuthSubject(RequestContext<CallToolRequestParams> context)
        => context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User?.FindFirstValue("sub")
           ?? throw new McpException("The request is not authenticated.");
}
