namespace HomeSystem.REST;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// The single document-level OpenAPI transformer for the host: document info and tags, the Authentik bearer
/// security scheme, and the shared <c>ProblemDetails</c> responses (400/401/403/404/422) produced by
/// <see cref="Shared.Infrastructure.Web.ApplicationExceptionHandler"/> and the auth middleware. Those responses
/// are added to every operation that does not already declare the status code, so an endpoint's own typed
/// result or explicit response metadata always wins.
/// </summary>
public sealed class HomeSystemDocumentTransformer : IOpenApiDocumentTransformer
{
    private const string BearerSchemeName = "Bearer";
    private const string ProblemJsonMediaType = "application/problem+json";

    /// <summary>Status code → description of the responses shared by every operation.</summary>
    private static readonly IReadOnlyDictionary<string, string> SharedProblemResponses =
        new Dictionary<string, string>
        {
            ["400"] = "Validation failed",
            ["401"] = "Unauthorized",
            ["403"] = "Forbidden",
            ["404"] = "Not found",
            ["422"] = "Business rule violation",
        };

    /// <inheritdoc />
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        ApplyInfo(document);
        ApplyBearerSecurity(document);
        await ApplySharedProblemResponsesAsync(document, context, ct);
    }

    private static void ApplyInfo(OpenApiDocument document)
    {
        document.Info = new()
        {
            Title = "Diet Planner API",
            Version = "v1",
            Description = "API for diet planning. Manage products, recipes, meal entries, and nutrition goals.",
        };

        document.Tags = new HashSet<OpenApiTag>
        {
            new() { Name = "Products", Description = "Nutritional product catalogue — create, search, update, and delete food products." },
            new() { Name = "Recipes", Description = "Recipes composed from products — create, search, update, and delete recipes with their ingredient lists." },
            new() { Name = "Meals", Description = "Daily meal log — record recipe servings against specific dates and meal types, and query aggregated nutrition summaries." },
            new() { Name = "Goals", Description = "Per-user daily nutrition targets — create or update calorie, protein, carbohydrate, fat, and fibre goals." },
            new() { Name = "Households", Description = "Households — the sharing boundary that groups the people living together." },
            new() { Name = "Persons", Description = "The local registry of people. Sync the signed-in account and read its Person record." },
            new() { Name = "Health", Description = "Service health check endpoint." },
        };
    }

    private static void ApplyBearerSecurity(OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token issued by Authentik. Pass the token value — the 'Bearer ' prefix is added automatically.",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSchemeName)] = [],
        });
    }

    private static async Task ApplySharedProblemResponsesAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct)
    {
        // 400 carries the `errors` extension; the other statuses are plain ProblemDetails.
        var validationSchema = await ReferenceSchemaAsync(document, context, typeof(HttpValidationProblemDetails), ct);
        var problemSchema = await ReferenceSchemaAsync(document, context, typeof(ProblemDetails), ct);

        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null)
                continue;

            foreach (var operation in path.Operations.Values)
            {
                operation.Responses ??= [];

                foreach (var (statusCode, description) in SharedProblemResponses)
                {
                    var schema = statusCode == "400" ? validationSchema : problemSchema;
                    operation.Responses.TryAdd(statusCode, new OpenApiResponse
                    {
                        Description = description,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            [ProblemJsonMediaType] = new() { Schema = schema },
                        },
                    });
                }
            }
        }
    }

    /// <summary>
    /// Resolves <paramref name="type"/> to a <c>$ref</c> into <c>components.schemas</c>, registering the schema
    /// there when no endpoint has already caused it to be emitted — so the generated TypeScript client sees one
    /// named <c>ProblemDetails</c> type instead of an inline copy per operation.
    /// </summary>
    private static async Task<IOpenApiSchema> ReferenceSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        Type type,
        CancellationToken ct)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        if (!document.Components.Schemas.ContainsKey(type.Name))
            document.Components.Schemas[type.Name] = await context.GetOrCreateSchemaAsync(type, null, ct);

        return new OpenApiSchemaReference(type.Name, document);
    }
}
