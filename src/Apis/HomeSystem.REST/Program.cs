using Budget.Api;
using Budget.Infrastructure;
using DietPlanner.Api;
using DietPlanner.Api.Mcp;
using DietPlanner.Infrastructure;
using HomeSystem.REST;
using Household.Api;
using Household.Infrastructure;
using Investments.Api;
using Investments.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using Notifications.Api;
using Operations.Api;
using Scalar.AspNetCore;
using Shared.Infrastructure.Cqrs.Extensions;
using Shared.Infrastructure.Messaging.Extensions;
using Shared.Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

// Wall clock — every handler, worker and inbox/outbox component reads time through this.
builder.Services.AddSingleton(TimeProvider.System);

// ==============================================
// Modules
// ==============================================
builder.Services.AddDietPlannerModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration, builder.Environment);
builder.Services.AddHouseholdModule(builder.Configuration);
builder.Services.AddBudgetModule(builder.Configuration);
builder.Services.AddInvestmentsModule(builder.Configuration);
builder.Services.AddOperationsModule();

// CQRS dispatcher chain — registered once, shared by every module's handlers.
builder.Services.AddCqrsDispatchers();

// ==============================================
// Messaging (integration-event bus + in-process transport)
// ==============================================
builder.Services
    .AddIntegrationEventBus()
    .UseInProcessTransport();

// ==============================================
// Authentication & Authorization
// ==============================================
var authentikConfig = builder.Configuration.GetSection("Authentication:Authentik");
var mcpEnabled = builder.Configuration.GetValue<bool>("Mcp:Enabled");

var authentication = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
authentication
    .AddJwtBearer(options =>
    {
        options.Authority = authentikConfig["Authority"];
        options.Audience = authentikConfig["Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };

        // SignalR clients pass the JWT via ?access_token=... on the WebSocket handshake
        // because the browser WebSocket API forbids custom headers.
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options => options.AddAdminPolicy());

// ==============================================
// MCP (disabled unless Mcp:Enabled) — docs/design/mcp/AUTH.md
// ==============================================
if (mcpEnabled)
{
    // MCP tokens come from their own OAuth client: own issuer and audience, own scheme. SPA tokens
    // cannot reach /mcp and MCP tokens cannot reach REST. Challenges go to the MCP metadata handler
    // so a 401 points the client at /.well-known/oauth-protected-resource (RFC 9728).
    var mcpAuthConfig = builder.Configuration.GetSection("Authentication:Mcp");
    authentication
        .AddJwtBearer(McpAuthorization.SchemeName, options =>
        {
            options.Authority = mcpAuthConfig["Authority"];
            options.Audience = mcpAuthConfig["Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };
        })
        .AddMcp(options => options.ResourceMetadata = new ProtectedResourceMetadata
        {
            AuthorizationServers = { mcpAuthConfig["Authority"]! },
            ScopesSupported = [.. DietPlannerMcp.Scopes],
            ResourceName = "Home System",
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddMcpEndpointPolicy();
        foreach (var scope in DietPlannerMcp.Scopes)
            options.AddMcpScopePolicy(scope);
    });

    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.Stateless = true)
        .AddAuthorizationFilters()
        .AddDietPlannerMcpTools();
}

// ==============================================
// CORS
// ==============================================
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// Vite dev server when nothing is configured; elsewhere no origin is allowed until Cors:AllowedOrigins is set.
if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    allowedOrigins = ["http://localhost:5173", "https://localhost:5173"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-SignalR-User-Agent")
              .AllowCredentials();
    });
});

// ==============================================
// Request Size Limits
// ==============================================
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024L * 1024L;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024L * 1024L;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// ==============================================
// OpenAPI
// ==============================================
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<HomeSystemDocumentTransformer>();
});

// ==============================================
// Error handling — one IExceptionHandler maps Shared.Abstractions exceptions to problem+json
// ==============================================
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

// ==============================================
// Build
// ==============================================
var app = builder.Build();

// ==============================================
// Middleware Pipeline
// ==============================================
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Diet Planner API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseMiddleware<SecurityHeadersMiddleware>();

// Expected (4xx / 499) exceptions are not faults: keep them out of the log and the error metrics.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = ApplicationExceptionHandler.ShouldSuppressDiagnostics,
});

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ==============================================
// Endpoints
// ==============================================
app.MapGet("/health", (TimeProvider clock) => TypedResults.Ok(new HealthResponse("healthy", clock.GetUtcNow().UtcDateTime, "1.0.0")))
    .WithName("HealthCheck")
    .WithTags("Health")
    .AllowAnonymous();

app.MapDietPlannerEndpoints();
app.MapNotificationsEndpoints();
app.MapNotificationChannelPreferencesEndpoints();
app.MapHouseholdEndpoints();
app.MapBudgetEndpoints();
app.MapInvestmentsEndpoints();
app.MapNotificationDeliveriesAdminEndpoints();
app.MapOperationsEndpoints();

if (mcpEnabled)
    app.MapMcp("/mcp").RequireAuthorization(McpAuthorization.EndpointPolicy);

// ==============================================
// Dev: auto-migrate on startup
// ==============================================
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateDietPlannerDatabaseAsync(app.Logger);
    app.Services.MigrateNotificationsDatabase();
    await app.Services.MigrateHouseholdDatabaseAsync(app.Logger);
    await app.Services.MigrateBudgetDatabaseAsync(app.Logger);
    await app.Services.MigrateInvestmentsDatabaseAsync(app.Logger, app.Lifetime.ApplicationStopping);
}

await app.RunAsync();

/// <summary>
/// Host entry point, exposed as a partial class so integration tests can bind
/// <c>WebApplicationFactory&lt;Program&gt;</c> to the real pipeline.
/// </summary>
public partial class Program { }

internal sealed record HealthResponse(string Status, DateTime Timestamp, string Version);
