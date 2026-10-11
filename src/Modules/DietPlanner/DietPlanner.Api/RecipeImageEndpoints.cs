namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.RemoveRecipeImage;
using DietPlanner.Application.Commands.UploadRecipeImage;
using DietPlanner.Application.Images;
using DietPlanner.Application.Queries.GetRecipeImage;
using DietPlanner.Application.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;
using Shared.Abstractions.Cqrs;

internal static class RecipeImageEndpoints
{
    internal static void MapRecipeImageEndpoints(this RouteGroupBuilder group)
    {
        // Bearer authentication supplies no ambient cookie credentials; CSRF tokens are not used.
        group.MapPut("/{id:guid}/image", Upload).WithName("UploadRecipeImage")
            .WithSummary("Upload or replace a saved recipe photo (JPEG, PNG, static WebP; 10 MiB / 25 MP)")
            .DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(11 * 1024 * 1024))
            .WithFormOptions(multipartBodyLengthLimit: 10 * 1024 * 1024, multipartHeadersLengthLimit: 4096,
                multipartBoundaryLengthLimit: 128, valueCountLimit: 4);
        group.MapGet("/{id:guid}/image", Read).WithName("GetRecipeImage")
            .AddOpenApiOperationTransformer(DocumentJpegAsync);
        group.MapGet("/{id:guid}/image/thumbnail", ReadThumbnail).WithName("GetRecipeImageThumbnail")
            .AddOpenApiOperationTransformer(DocumentJpegAsync);
        group.MapDelete("/{id:guid}/image", Remove).WithName("RemoveRecipeImage");
    }

    private static async Task<Ok<RecipeImageDto>> Upload(Guid id, IFormFile file,
        ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await using Stream input = file.OpenReadStream();
        RecipeImageDto image = await dispatcher.SendAsync<UploadRecipeImageCommand, RecipeImageDto>(
            new UploadRecipeImageCommand(id, RecipeEndpoints.GetUserId(user), input, file.ContentType, file.FileName), ct);
        return TypedResults.Ok(image);
    }

    private static Task<Results<FileStreamHttpResult, NotFound>> Read(Guid id, ClaimsPrincipal user,
        HttpContext http, IQueryDispatcher dispatcher, CancellationToken ct)
        => ReadAsync(id, user, http, dispatcher, false, ct);

    private static Task<Results<FileStreamHttpResult, NotFound>> ReadThumbnail(Guid id, ClaimsPrincipal user,
        HttpContext http, IQueryDispatcher dispatcher, CancellationToken ct)
        => ReadAsync(id, user, http, dispatcher, true, ct);

    private static async Task<Results<FileStreamHttpResult, NotFound>> ReadAsync(Guid id, ClaimsPrincipal user,
        HttpContext http, IQueryDispatcher dispatcher, bool thumbnail, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "private, no-store";
        http.Response.Headers.Pragma = "no-cache";
        http.Response.Headers.XContentTypeOptions = "nosniff";
        ImageObjectRead? image = await dispatcher.SendAsync<GetRecipeImageQuery, ImageObjectRead?>(
            new GetRecipeImageQuery(id, RecipeEndpoints.GetUserId(user), thumbnail), ct);
        if (image is null)
            return TypedResults.NotFound();
        http.Response.RegisterForDispose(image);
        return TypedResults.Stream(image.Content, "image/jpeg", enableRangeProcessing: false);
    }

    private static async Task<NoContent> Remove(Guid id, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new RemoveRecipeImageCommand(id, RecipeEndpoints.GetUserId(user)), ct);
        return TypedResults.NoContent();
    }

    private static Task DocumentJpegAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        // FileStreamHttpResult supplies no success-response metadata; document the binary JPEG contract.
        operation.Responses ??= new OpenApiResponses();
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Normalized JPEG photo",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["image/jpeg"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } },
            },
        };
        return Task.CompletedTask;
    }
}
