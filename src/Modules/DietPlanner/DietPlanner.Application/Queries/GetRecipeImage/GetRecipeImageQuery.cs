namespace DietPlanner.Application.Queries.GetRecipeImage;

using DietPlanner.Application.Storage;
using Shared.Abstractions.Cqrs;

/// <summary>Reads the current photo after checking current recipe access.</summary>
/// <param name="Id">Recipe.</param>
/// <param name="UserId">Caller.</param>
/// <param name="Thumbnail">Whether to read the bounded thumbnail.</param>
public sealed record GetRecipeImageQuery(Guid Id, string UserId, bool Thumbnail) : IQuery<ImageObjectRead?>;
