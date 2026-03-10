using DietPlanner.Api.Data;
using DietPlanner.Api.Features.Recipes;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// Database Configuration
// ==============================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not configured");

    // Note: EnableRetryOnFailure is disabled because it conflicts with user-initiated transactions
    // in the import system. The import executor handles its own transaction management.
    options.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

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

        if (builder.Environment.IsDevelopment())
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine("Token validated successfully");
                    return Task.CompletedTask;
                }
            };
        }
    });

builder.Services.AddAuthorization();

// ==============================================
// CORS Configuration
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
// Rate Limiting (per-user partitioning)
// ==============================================
builder.Services.AddRateLimiter(options =>
{
    var importLimit = builder.Configuration.GetValue<int>("RateLimiting:ImportRequestsPerMinute", 25);
    var apiLimit = builder.Configuration.GetValue<int>("RateLimiting:ApiRequestsPerMinute", 100);

    // Import endpoints - 25 requests per user per minute
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

    // General API endpoints - 100 requests per user per minute
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
    var maxSizeMB = builder.Configuration.GetValue<int>("RateLimiting:MaxImportSizeMB", 5);
    options.MultipartBodyLengthLimit = maxSizeMB * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    var maxSizeMB = builder.Configuration.GetValue<int>("RateLimiting:MaxImportSizeMB", 5);
    options.Limits.MaxRequestBodySize = maxSizeMB * 1024 * 1024;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// ==============================================
// API Documentation (OpenAPI)
// ==============================================
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new()
        {
            Title = "Diet Planner API",
            Version = "v1",
            Description = "API for diet planning with JSON import capabilities. " +
                          "Manage products, recipes, and diet plans with full nutrition tracking."
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
builder.Services.AddEndpointsApiExplorer();

// ==============================================
// FluentValidation
// ==============================================
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ==============================================
// Services
// ==============================================
builder.Services.AddScoped<DietPlanner.Api.Features.Products.IProductService,
    DietPlanner.Api.Features.Products.ProductService>();

builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<INutritionCalculator, NutritionCalculator>();

builder.Services.AddScoped<DietPlanner.Api.Features.Meals.IMealService,
    DietPlanner.Api.Features.Meals.MealService>();

builder.Services.AddScoped<DietPlanner.Api.Features.Goals.IGoalService,
    DietPlanner.Api.Features.Goals.GoalService>();

// Import services
builder.Services.AddScoped<DietPlanner.Api.Features.DietPlans.Import.IImportValidator,
    DietPlanner.Api.Features.DietPlans.Import.ImportValidator>();
builder.Services.AddScoped<DietPlanner.Api.Features.DietPlans.Import.IImportExecutor,
    DietPlanner.Api.Features.DietPlans.Import.ImportExecutor>();

// ==============================================
// Build Application
// ==============================================
var app = builder.Build();

// ==============================================
// Middleware Pipeline
// ==============================================

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Diet Planner API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseMiddleware<DietPlanner.Api.Common.Middleware.ErrorHandlingMiddleware>();

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

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health");

app.MapGet("/api/v1/info", () => Results.Ok(new
{
    version = "1.0.0",
    name = "Diet Planner API",
    description = "API for diet planning with JSON import capabilities",
    features = new[]
    {
        "Product management",
        "Recipe management",
        "Diet plan import",
        "Nutrition calculation",
        "Shopping list generation"
    }
}))
.WithName("ApiInfo")
.WithTags("Info");

DietPlanner.Api.Features.Products.ProductEndpoints.MapProductEndpoints(app);
DietPlanner.Api.Features.Recipes.RecipeEndpoints.MapRecipeEndpoints(app);
DietPlanner.Api.Features.Meals.MealEndpoints.MapMealEndpoints(app);
DietPlanner.Api.Features.Goals.GoalEndpoints.MapGoalEndpoints(app);

// ==============================================
// Database Migration on Startup (Development only)
// ==============================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying database migrations");
    }
}

app.Run();

public partial class Program { }
