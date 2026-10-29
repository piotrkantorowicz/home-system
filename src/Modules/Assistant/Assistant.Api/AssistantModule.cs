namespace Assistant.Api;

using System.ClientModel;
using System.ClientModel.Primitives;
using Anthropic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

/// <summary>
/// The Assistant module: an in-app agent that answers through the modules' MCP tools, called in-process
/// as the signed-in user. It owns no data, so it has only an Api layer.
/// Design: <c>docs/design/assistant/README.md</c>.
/// </summary>
public static class AssistantModule
{
    /// <summary>Named <see cref="HttpClient"/> the OpenAI-compatible providers send through.</summary>
    public const string HttpClientName = "Assistant";

    private const string DefaultAnthropicModel = "claude-haiku-5-5";
    private static readonly Uri XaiEndpoint = new("https://api.x.ai/v1");

    /// <summary>Registers the assistant when <c>Assistant:Enabled</c>; the host registers the tools.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="environment">The host environment; the <c>Fake</c> provider is refused in Production.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAssistantModule(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection(AssistantOptions.SectionName).Get<AssistantOptions>() ?? new AssistantOptions();
        options.WritesEnabled = configuration.GetValue("Mcp:WritesEnabled", true);
        options.ImportsEnabled = configuration.GetValue("Mcp:ImportsEnabled", false);
        services.AddSingleton(options);
        if (!options.Enabled)
            return services;

        if (options.MaxToolRounds < 1 || options.MaxOutputTokens < 1 || options.DailyTokenBudget < 1
            || options.RequestsPerMinute < 1 || options.MaxHistoryTokens < 1)
            throw new InvalidOperationException(
                "Assistant:MaxToolRounds, MaxOutputTokens, DailyTokenBudget, RequestsPerMinute and MaxHistoryTokens must be positive.");

        // The scripted model for E2E runs; it needs no key and must never answer real users.
        if (options.Provider == "Fake")
        {
            if (environment.IsProduction())
                throw new InvalidOperationException("Assistant:Provider 'Fake' is for E2E runs and is refused in Production.");
            services.AddSingleton<IChatClient>(sp => new FakeChatClient(sp.GetRequiredService<TimeProvider>()));
        }
        else
        {
            AddChatClient(services, options);
        }

        services.AddSingleton<AssistantUsage>();
        services.AddSingleton<PendingActions>();
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
                string model = string.IsNullOrWhiteSpace(options.Model) ? DefaultAnthropicModel : options.Model;
                services.AddSingleton<IAnthropicClient>(_ => new AnthropicClient { ApiKey = options.ApiKey });
                services.AddSingleton<IChatClient>(sp => new AnthropicPromptCaching(
                    sp.GetRequiredService<IAnthropicClient>().AsIChatClient(model, options.MaxOutputTokens)));
                break;

            // OpenAI and xAI cache prompts automatically, so they need no caching wrapper.
            // ponytail: Chat Completions, which xAI now lists as legacy next to its Responses API. Both
            // providers still serve it and it keeps the call stateless; move to Responses (with store off)
            // if xAI retires it.
            case "OpenAI":
            case "XAI":
                if (string.IsNullOrWhiteSpace(options.Model))
                    throw new InvalidOperationException($"Assistant:Model is required for Assistant:Provider '{options.Provider}'.");

                Uri? endpoint = options.BaseUrl ?? (options.Provider == "XAI" ? XaiEndpoint : null);
                services.AddHttpClient(HttpClientName);
                services.AddSingleton<IChatClient>(sp => new OpenAIClient(
                        new ApiKeyCredential(options.ApiKey),
                        new OpenAIClientOptions
                        {
                            Endpoint = endpoint,
                            Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)),
                        })
                    .GetChatClient(options.Model)
                    .AsIChatClient());
                break;

            default:
                throw new InvalidOperationException(
                    $"Assistant:Provider '{options.Provider}' is not supported. Use Anthropic, OpenAI or XAI.");
        }
    }
}
