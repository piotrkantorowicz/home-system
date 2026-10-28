namespace Assistant.Api;

using Anthropic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The Assistant module: an in-app agent that answers through the modules' MCP tools, called in-process
/// as the signed-in user. It owns no data, so it has only an Api layer.
/// Design: <c>docs/design/assistant/README.md</c>.
/// </summary>
public static class AssistantModule
{
    /// <summary>Registers the assistant when <c>Assistant:Enabled</c>; the host registers the tools.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAssistantModule(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AssistantOptions.SectionName).Get<AssistantOptions>() ?? new AssistantOptions();
        services.AddSingleton(options);
        if (!options.Enabled)
            return services;

        if (options.MaxToolRounds < 1 || options.MaxOutputTokens < 1)
            throw new InvalidOperationException("Assistant:MaxToolRounds and Assistant:MaxOutputTokens must be positive.");

        AddChatClient(services, options);
        services.AddSingleton<AssistantAgent>();
        return services;
    }

    /// <summary>Maps the assistant endpoint when <c>Assistant:Enabled</c>; otherwise it answers 404.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
        => app.ServiceProvider.GetRequiredService<AssistantOptions>().Enabled ? app.MapAssistantEndpointsGroup() : app;

    // One provider and model per deployment, checked at startup. The only provider-specific code.
    // Clients come from factories so the container owns and disposes them.
    private static void AddChatClient(IServiceCollection services, AssistantOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Assistant:ApiKey is required when Assistant:Enabled is true.");

        switch (options.Provider)
        {
            case "Anthropic":
                services.AddSingleton<IAnthropicClient>(_ => new AnthropicClient { ApiKey = options.ApiKey });
                services.AddSingleton<IChatClient>(sp => new AnthropicPromptCaching(
                    sp.GetRequiredService<IAnthropicClient>().AsIChatClient(options.Model, options.MaxOutputTokens)));
                break;
            default:
                throw new InvalidOperationException($"Assistant:Provider '{options.Provider}' is not supported.");
        }
    }
}
