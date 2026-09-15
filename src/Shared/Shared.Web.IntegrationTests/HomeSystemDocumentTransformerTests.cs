namespace Shared.Web.IntegrationTests;

using System.Text.Json;
using HomeSystem.REST;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// <see cref="HomeSystemDocumentTransformer"/> owns the whole document-level OpenAPI shape: info, bearer scheme,
/// and the shared problem responses on every operation — without overriding what an endpoint declares itself.
/// </summary>
public sealed class HomeSystemDocumentTransformerTests
{
    private static readonly string[] SharedStatuses = ["400", "401", "403", "404", "422"];

    /// <summary>The real host's document: every operation documents the five shared problem responses.</summary>
    [Fact]
    public async Task Transform_OnTheRealHost_AddsSharedProblemResponsesToEveryOperation()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            // Outside Development the bearer handler refuses an http:// authority on every request.
            builder.UseSetting("Authentication:Authentik:Authority", "https://auth.example.test");
            // Only the document is under test — no outbox worker or reminder tick touching a database.
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        });
        using var client = factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var paths = document.RootElement.GetProperty("paths");
        paths.EnumerateObject().ShouldNotBeEmpty();

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var responses = operation.Value.GetProperty("responses");
                foreach (var status in SharedStatuses)
                {
                    // An endpoint may declare a status itself (with or without a body); it must at least be there.
                    responses.TryGetProperty(status, out _)
                        .ShouldBeTrue($"{operation.Name.ToUpperInvariant()} {path.Name} should document {status}");
                }
            }
        }

        // /health declares only its 200, so its shared responses come from the transformer alone.
        var health = paths.GetProperty("/health").GetProperty("get").GetProperty("responses");
        SchemaRef(health, "400").ShouldEndWith("/HttpValidationProblemDetails");
        SchemaRef(health, "404").ShouldEndWith("/ProblemDetails");

        document.RootElement.GetProperty("components").GetProperty("securitySchemes")
            .TryGetProperty("Bearer", out _).ShouldBeTrue();
    }

    /// <summary>400 points at the validation schema (with <c>errors</c>); the other statuses at plain ProblemDetails.</summary>
    [Fact]
    public async Task Transform_OnAnOperation_UsesValidationSchemaFor400AndProblemDetailsOtherwise()
    {
        await using var app = await StartMinimalHostAsync(endpoints =>
            endpoints.MapGet("/things", () => TypedResults.Ok("ok")));
        using var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var responses = document.RootElement.GetProperty("paths").GetProperty("/things").GetProperty("get")
            .GetProperty("responses");
        SchemaRef(responses, "400").ShouldEndWith("/HttpValidationProblemDetails");
        SchemaRef(responses, "401").ShouldEndWith("/ProblemDetails");
        SchemaRef(responses, "403").ShouldEndWith("/ProblemDetails");
        SchemaRef(responses, "404").ShouldEndWith("/ProblemDetails");
        SchemaRef(responses, "422").ShouldEndWith("/ProblemDetails");
        responses.TryGetProperty("200", out _).ShouldBeTrue();
    }

    /// <summary>A status code the endpoint documents itself is left untouched; the rest are filled in.</summary>
    [Fact]
    public async Task Transform_WhenOperationDeclaresAStatus_DoesNotReplaceIt()
    {
        await using var app = await StartMinimalHostAsync(endpoints =>
            endpoints.MapGet("/things/{id}", (int id) => TypedResults.Ok("ok"))
                .Produces<ThingMissing>(StatusCodes.Status404NotFound));
        using var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var responses = document.RootElement.GetProperty("paths").GetProperty("/things/{id}").GetProperty("get")
            .GetProperty("responses");
        responses.GetProperty("404").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().ShouldEndWith("/ThingMissing");
        SchemaRef(responses, "400").ShouldEndWith("/HttpValidationProblemDetails");
        SchemaRef(responses, "422").ShouldEndWith("/ProblemDetails");
    }

    private static string? SchemaRef(JsonElement responses, string status)
        => responses.GetProperty(status).GetProperty("content").GetProperty("application/problem+json")
            .GetProperty("schema").GetProperty("$ref").GetString();

    private static async Task<WebApplication> StartMinimalHostAsync(Action<IEndpointRouteBuilder> map)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<HomeSystemDocumentTransformer>());

        var app = builder.Build();
        app.MapOpenApi();
        map(app);
        await app.StartAsync();
        return app;
    }

    /// <summary>A response body type that is not ProblemDetails, so a replaced 404 is visible in the document.</summary>
    private sealed record ThingMissing(int Id);
}
