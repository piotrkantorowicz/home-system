import { useRecipeImageScope } from '../../hooks/useRecipeImageScope';

import { RecipePhotoContent } from './RecipePhotoContent';

import type { RecipeDto } from '../../api/hooks/useRecipes';

interface RecipePhotoProps {
  recipe: RecipeDto;
}

export function RecipePhoto({ recipe }: RecipePhotoProps) {
  const scope = useRecipeImageScope();
  if (!scope) return null;
  return (
    <RecipePhotoContent
      key={`${scope}:${recipe.id}:${String(recipe.canEdit)}`}
      recipe={recipe}
      scope={scope}
    />
  );
}
