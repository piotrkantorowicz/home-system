namespace Shared.Infrastructure.Web;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Wires the caller into tool calls on both paths: <c>/mcp</c> requests and <see cref="McpToolInvoker"/>.
/// </summary>
public static class McpToolHosting
{
    /// <summary>Registers <see cref="McpToolUser"/>, binds it from each <c>/mcp</c> call and adds <see cref="McpToolInvoker"/>.</summary>
    /// <param name="builder">The host's MCP server builder.</param>
    /// <returns><paramref name="builder"/> for chaining.</returns>
    public static IMcpServerBuilder AddInProcessToolCalls(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<McpToolUser>();
        builder.Services.AddSingleton<McpToolInvoker>();

        return builder.WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, ct) =>
        {
            if (context.User is { } user)
                context.Services!.GetRequiredService<McpToolUser>().Set(user);
            return await next(context, ct).ConfigureAwait(false);
        }));
    }
}
