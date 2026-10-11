namespace DietPlanner.Application.Commands.UploadRecipeImage;

using DietPlanner.Application.Images;
using Shared.Abstractions.Cqrs;

/// <summary>Uploads a photo for an already saved recipe.</summary>
/// <param name="Id">Recipe.</param>
/// <param name="UserId">Caller subject.</param>
/// <param name="Content">Caller-owned input stream.</param>
/// <param name="ContentType">Declared type.</param>
/// <param name="FileName">Declared filename.</param>
public sealed record UploadRecipeImageCommand(Guid Id, string UserId, Stream Content,
    string ContentType, string FileName) : ICommand<RecipeImageDto>;
