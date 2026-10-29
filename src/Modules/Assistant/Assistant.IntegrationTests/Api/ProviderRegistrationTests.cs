namespace Assistant.IntegrationTests.Api;

using System.Net.ServerSentEvents;
using System.Text.Json;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#732 — one provider per deployment from configuration; no live model calls.</summary>
public sealed class ProviderRegistrationTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>
{
    /// <summary>
    /// OpenAI and xAI run through the OpenAI client against their own endpoint with the configured key and
    /// model, and their token usage is counted like any other provider's.
    /// </summary>
    /// <param name="provider">Configured provider.</param>
    /// <param name="host">The endpoint host it must call.</param>
    [Theory]
    [InlineData("OpenAI", "api.openai.com")]
    [InlineData("XAI", "api.x.ai")]
    public async Task SendMessage_OpenAICompatibleProvider_CallsItsEndpointAndCountsUsage(string provider, string host)
    {
        using var factory = new AssistantApiFactory(
            databases, new Dictionary<string, string?> { ["Assistant:Provider"] = provider, ["Assistant:Model"] = "test-model" }, realProvider: true);

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(
            factory.CreateClientFor($"auth|{Guid.CreateVersion7():N}"), new { message = "Hello" });

        Data(events, "text").GetProperty("text").GetString().ShouldBe("Hi");
        JsonElement usage = Data(events, "usage");
        usage.GetProperty("inputTokens").GetInt64().ShouldBe(40);
        usage.GetProperty("outputTokens").GetInt64().ShouldBe(2);
        usage.GetProperty("todayTokens").GetInt64().ShouldBe(42);

        (HttpRequestMessage request, string body) = factory.Provider.Requests.Single();
        request.RequestUri!.Host.ShouldBe(host);
        request.Headers.Authorization!.ToString().ShouldBe("Bearer test-key");
        JsonDocument.Parse(body).RootElement.GetProperty("model").GetString().ShouldBe("test-model");
    }

    /// <summary>The default provider is Anthropic with Claude Haiku 5.5, behind the prompt-caching wrapper.</summary>
    [Fact]
    public void Services_DefaultProvider_IsAnthropicHaiku()
    {
        using var factory = new AssistantApiFactory(databases, realProvider: true);

        IChatClient chat = factory.Services.GetRequiredService<IChatClient>();

        ChatClientMetadata metadata = chat.GetService<ChatClientMetadata>()!;
        metadata.ProviderName!.ShouldContain("anthropic", Case.Insensitive);
        metadata.DefaultModelId.ShouldBe("claude-haiku-5-5");
    }

    /// <summary>An unknown provider or incomplete settings stop the host at startup with a readable message.</summary>
    /// <param name="key">Setting to override.</param>
    /// <param name="value">Its value.</param>
    /// <param name="message">Part of the expected message.</param>
    [Theory]
    [InlineData("Assistant:Provider", "Gemini", "not supported")]
    [InlineData("Assistant:ApiKey", "", "ApiKey is required")]
    [InlineData("Assistant:Provider", "OpenAI", "Model is required")]
    public void Startup_InvalidProviderSettings_FailsWithReadableMessage(string key, string value, string message)
    {
        using var factory = new AssistantApiFactory(databases, new Dictionary<string, string?> { [key] = value }, realProvider: true);

        Exception failure = Should.Throw<Exception>(() => factory.Services);

        Messages(failure).ShouldContain(m => m.Contains(message, StringComparison.Ordinal));
    }

    private static IEnumerable<string> Messages(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
            yield return exception.Message;
    }

    private static JsonElement Data(List<SseItem<string>> events, string type)
        => JsonDocument.Parse(events.First(e => e.EventType == type).Data).RootElement;
}
