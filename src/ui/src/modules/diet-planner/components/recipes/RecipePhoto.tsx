import { useHousehold } from '@modules/household';
import { useAuth } from 'react-oidc-context';

import { RecipePhotoContent } from './RecipePhotoContent';

import type { RecipeDto } from '../../api/hooks/useRecipes';

interface RecipePhotoProps {
  recipe: RecipeDto;
}

export function RecipePhoto({ recipe }: RecipePhotoProps) {
  const auth = useAuth();
  const { household, myPersonId, myRole } = useHousehold();
  const subject = auth.user?.profile.sub;
  if (!auth.isAuthenticated || !subject) return null;
  const scope = JSON.stringify([subject, household?.id, myPersonId, myRole]);
  return (
    <RecipePhotoContent
      key={`${scope}:${recipe.id}:${String(recipe.canEdit)}`}
      recipe={recipe}
      scope={scope}
    />
  );
}
