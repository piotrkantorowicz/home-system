namespace DietPlanner.Application.Images;

internal static class RecipeImageKeys
{
    internal static string Detail(Guid version) => $"recipe-images/{version:N}/detail.jpg";
    internal static string Thumbnail(Guid version) => $"recipe-images/{version:N}/thumbnail.jpg";
}
