namespace Budget.Mcp;

using ModelContextProtocol;

/// <summary>
/// The tools call a command's <c>ClientRequestId</c> <c>operationKey</c>. An all-zero key is rejected here so
/// the agent reads the parameter it actually sent, not the command's field name.
/// </summary>
internal static class OperationKey
{
    /// <summary>Returns <paramref name="key"/>, or throws the tool error for an empty one.</summary>
    /// <param name="key">The operation key the agent passed.</param>
    /// <returns>The same key.</returns>
    public static Guid Require(Guid key)
        => key == Guid.Empty ? throw new McpException("operationKey is required: use a fresh GUID, not all zeros.") : key;
}
