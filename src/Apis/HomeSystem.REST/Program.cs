using DietPlanner.Api;
using DietPlanner.Infrastructure;
using Household.Api;
using Household.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Notifications.Api;
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

// ==============================================
// CORS
// ==============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
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
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new()
        {
            Title = "Diet Planner API",
            Version = "v1",
            Description = "API for diet planning. Manage products, recipes, meal entries, and nutrition goals."
        };

        document.Tags = new HashSet<Microsoft.OpenApi.OpenApiTag>
        {
            new() { Name = "Products", Description = "Nutritional product catalogue — create, search, update, and delete food products." },
            new() { Name = "Recipes", Description = "Recipes composed from products — create, search, update, and delete recipes with their ingredient lists." },
            new() { Name = "Meals", Description = "Daily meal log — record recipe servings against specific dates and meal types, and query aggregated nutrition summaries." },
            new() { Name = "Goals", Description = "Per-user daily nutrition targets — create or update calorie, protein, carbohydrate, fat, and fibre goals." },
            new() { Name = "Households", Description = "Households — the sharing boundary that groups the people living together." },
            new() { Name = "Persons", Description = "The local registry of people. Sync the signed-in account and read its Person record." },
            new() { Name = "Health", Description = "Service health check endpoint." }
        };

        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token issued by Authentik. Pass the token value — the 'Bearer ' prefix is added automatically."
        };

        document.Security ??= [];
        document.Security.Add(new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

// ==============================================
// Error handling middleware
// ==============================================
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
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

// ==============================================
// Dev: auto-migrate on startup
// ==============================================
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateDietPlannerDatabaseAsync(app.Logger);
    app.Services.MigrateNotificationsDatabase();
    await app.Services.MigrateHouseholdDatabaseAsync(app.Logger);
}

await app.RunAsync();

/// <summary>
/// Host entry point, exposed as a partial class so integration tests can bind
/// <c>WebApplicationFactory&lt;Program&gt;</c> to the real pipeline.
/// </summary>
public partial class Program { }

internal sealed record HealthResponse(string Status, DateTime Timestamp, string Version);
