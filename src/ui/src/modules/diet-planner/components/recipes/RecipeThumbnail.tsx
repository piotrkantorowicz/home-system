import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import { recipeImageOptions } from '../../api/hooks/useRecipeImage';
import { useObjectUrl } from '../../hooks/useObjectUrl';
import { useRecipeImageScope } from '../../hooks/useRecipeImageScope';

import type { RecipeDto } from '../../api/hooks/useRecipes';

interface RecipeThumbnailProps {
  id: string;
  image: NonNullable<RecipeDto['image']>;
}

export function RecipeThumbnail({ id, image }: RecipeThumbnailProps) {
  const scope = useRecipeImageScope();
  if (!scope) return null;
  return (
    <ThumbnailContent key={`${scope}:${id}:${image.version}`} id={id} image={image} scope={scope} />
  );
}

function ThumbnailContent({ id, image, scope }: RecipeThumbnailProps & { scope: string }) {
  const query = useQuery(recipeImageOptions(id, image.version, scope, 'thumbnail'));
  const [broken, setBroken] = useState(false);
  const url = useObjectUrl(query.isError || broken ? null : query.data);
  if (broken || query.isError || query.data === null) return null;
  // Hold the slot while the bytes load so the title does not shift once the photo arrives.
  if (!url) return <span aria-hidden="true" className="bg-muted size-12 shrink-0 rounded-lg" />;
  return (
    <img
      src={url}
      alt=""
      aria-hidden="true"
      width={48}
      height={48}
      decoding="async"
      className="size-12 shrink-0 rounded-lg object-cover"
      onError={() => {
        setBroken(true);
      }}
    />
  );
}
