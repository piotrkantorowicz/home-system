namespace Assistant.IntegrationTests.Infrastructure;

using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using Household.Infrastructure;
using Investments.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

/// <summary>
/// The real host with the assistant on, <c>/mcp</c> off, the model replaced by <see cref="Model"/>,
/// Investments and Household on Testcontainers Postgres and header-driven test auth.
/// </summary>
/// <param name="databases">The class's databases.</param>
/// <param name="settings">Configuration overrides on top of the assistant defaults below.</param>
public sealed class AssistantApiFactory(AssistantDatabaseFixture databases, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    private Task? _migrated;

    /// <summary>The scripted model every request in this host talks to.</summary>
    public ScriptedChatClient Model { get; } = new();

    /// <summary>A client acting as <paramref name="subject"/>.</summary>
    /// <param name="subject">Auth subject; unique per test.</param>
    /// <returns>The client.</returns>
    public HttpClient CreateClientFor(string subject)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject);
        return client;
    }

    /// <summary>A signed-in user who owns a household, so wallets and the read tools work for them.</summary>
    /// <returns>The user's client.</returns>
    public async Task<HttpClient> HouseholdOwnerAsync()
    {
        await (_migrated ??= MigrateAsync());
        var client = CreateClientFor($"auth|{Guid.CreateVersion7():N}");
        (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/households", new { name = "Home" }, Ct)).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Creates a wallet over REST.</summary>
    /// <param name="client">The owner.</param>
    /// <param name="name">Wallet name.</param>
    /// <returns>A task.</returns>
    public static async Task CreateWalletAsync(HttpClient client, string name)
        => (await client.PostAsJsonAsync("/api/investments/wallets",
            new { name, institution = "XTB", kind = "Brokerage", currency = "PLN", visibility = "Private" }, Ct)).EnsureSuccessStatusCode();

    /// <summary>Sends one assistant message and reads the whole event stream.</summary>
    /// <param name="client">The caller.</param>
    /// <param name="body">Request body.</param>
    /// <returns>The events, in order.</returns>
    public static async Task<List<SseItem<string>>> SendAsync(HttpClient client, object body)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/assistant/messages", body, Ct);
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(Ct);
        List<SseItem<string>> events = [];
        await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync(Ct))
            events.Add(item);
        return events;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Investments", databases.InvestmentsConnectionString);
        builder.UseSetting("ConnectionStrings:Household", databases.HouseholdConnectionString);
        builder.UseSetting("Investments:NbpFx:Enabled", "false");
        builder.UseSetting("Assistant:Enabled", "true");
        builder.UseSetting("Assistant:ApiKey", "test-key");
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            builder.UseSetting(key, value);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 21, 30, 0, TimeSpan.Zero)));
            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient>(Model);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private async Task MigrateAsync()
    {
        await Services.MigrateHouseholdDatabaseAsync(NullLogger.Instance);
        await Services.MigrateInvestmentsDatabaseAsync(NullLogger.Instance, Ct);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
