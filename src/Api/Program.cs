using DietPlanner.Api;
using DietPlanner.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Middleware;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// Modules
// ==============================================
builder.Services.AddDietPlannerModule(builder.Configuration);

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
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization")
              .AllowCredentials();
    });
});

// ==============================================
// Rate Limiting
// ==============================================
builder.Services.AddRateLimiter(options =>
{
    var importLimit = builder.Configuration.GetValue("RateLimiting:ImportRequestsPerMinute", 25);
    var apiLimit = builder.Configuration.GetValue("RateLimiting:ApiRequestsPerMinute", 100);

    options.AddPolicy("import", context =>
    {
        var partitionKey = context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = importLimit,
                QueueLimit = 0
            });
    });

    options.AddPolicy("api", context =>
    {
        var partitionKey = context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = apiLimit,
                QueueLimit = 10
            });
    });

    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
            ? (double?)retryAfterValue.TotalSeconds
            : null;

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded. Please try again later.",
            retryAfter
        });
    };
});

// ==============================================
// Request Size Limits
// ==============================================
builder.Services.Configure<FormOptions>(options =>
{
    var maxSizeMB = builder.Configuration.GetValue("RateLimiting:MaxImportSizeMB", 5);
    options.MultipartBodyLengthLimit = maxSizeMB * 1024L * 1024L;
});

builder.WebHost.ConfigureKestrel(options =>
{
    var maxSizeMB = builder.Configuration.GetValue("RateLimiting:MaxImportSizeMB", 5);
    options.Limits.MaxRequestBodySize = maxSizeMB * 1024L * 1024L;
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

        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token from Authentik"
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ==============================================
// Endpoints
// ==============================================
app.MapGet("/health", () => TypedResults.Ok(new HealthResponse("healthy", DateTime.UtcNow, "1.0.0")))
    .WithName("HealthCheck")
    .WithTags("Health")
    .AllowAnonymous();

app.MapDietPlannerEndpoints();

// ==============================================
// Dev: auto-migrate on startup
// ==============================================
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateDietPlannerDatabaseAsync(app.Logger);
}

app.Run();

public partial class Program { }

internal sealed record HealthResponse(string Status, DateTime Timestamp, string Version);
