import {
  RecipeImageError,
  recipeImageOptions,
  useChangeRecipeImage,
} from '@modules/diet-planner/api/hooks/useRecipeImage';
import { useObjectUrl } from '@modules/diet-planner/hooks/useObjectUrl';
import { Button, Field, Input } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { RecipeDto } from '../../api/hooks/useRecipes';
import type { ChangeEvent } from 'react';

interface RecipePhotoContentProps {
  recipe: RecipeDto;
  scope: string;
}

export function RecipePhotoContent({ recipe, scope }: RecipePhotoContentProps) {
  const { t } = useTranslation('diet-planner');
  const toast = useToast();
  const inputId = useId();
  const input = useRef<HTMLInputElement>(null);
  const focusAfterSave = useRef(false);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [brokenUrl, setBrokenUrl] = useState<string | null>(null);
  const image = useQuery({
    ...recipeImageOptions(recipe.id, recipe.image?.version ?? '', scope),
    enabled: !!recipe.image,
  });
  const change = useChangeRecipeImage(recipe.id, scope);
  const denied =
    change.error instanceof RecipeImageError && [401, 403].includes(change.error.status);
  const imageUrl = useObjectUrl(image.isError || denied ? null : image.data);
  const previewUrl = useObjectUrl(denied ? null : file);
  const visibleUrl = imageUrl && imageUrl !== brokenUrl ? imageUrl : undefined;
  const busy = change.isPending;
  useEffect(() => {
    if (!busy && focusAfterSave.current) {
      input.current?.focus();
      focusAfterSave.current = false;
    }
  }, [busy]);

  const clearSelection = () => {
    setFile(null);
    setError(null);
    if (input.current) {
      input.current.value = '';
      if (input.current.disabled) focusAfterSave.current = true;
      else input.current.focus();
    }
  };
  const handleSelect = (event: ChangeEvent<HTMLInputElement>) => {
    const selected = event.target.files?.[0];
    if (!selected) return;
    change.reset();
    if (
      !['image/jpeg', 'image/png', 'image/webp'].includes(selected.type) ||
      selected.size > 10 * 1024 * 1024 ||
      selected.size === 0
    ) {
      setError(t('recipe_photo.invalid_file'));
      setFile(null);
      event.target.value = '';
      return;
    }
    setError(null);
    setFile(selected);
  };
  const handleChange = (selected: File | null) => {
    setError(null);
    change.mutate(selected, {
      onSuccess: () => {
        clearSelection();
        toast.success(t(selected ? 'recipe_photo.upload_success' : 'recipe_photo.remove_success'));
      },
      onError: (failure) => {
        const status = failure instanceof RecipeImageError ? failure.status : 0;
        if ([401, 403, 404].includes(status)) setFile(null);
        setError(
          t(
            [401, 403, 404].includes(status)
              ? 'recipe_photo.access_error'
              : [400, 413, 415, 422].includes(status)
                ? 'recipe_photo.invalid_file'
                : status === 409
                  ? 'recipe_photo.conflict'
                  : selected
                    ? 'recipe_photo.upload_error'
                    : 'recipe_photo.remove_error',
          ),
        );
      },
    });
  };

  if (!recipe.canEdit && !recipe.image) return null;

  return (
    <section aria-label={t('recipe_photo.title')} aria-busy={busy} className="flex flex-col gap-3">
      {visibleUrl ? (
        <img
          src={visibleUrl}
          alt={t('recipe_photo.alt', { name: recipe.name })}
          className="max-h-96 w-full rounded-lg object-contain"
          onError={() => {
            setBrokenUrl(imageUrl ?? null);
          }}
        />
      ) : null}
      {recipe.image && !visibleUrl ? (
        <p className="text-muted-foreground text-sm" role="status">
          {image.isPending && !denied ? t('recipe_photo.loading') : t('recipe_photo.unavailable')}
        </p>
      ) : null}
      {recipe.canEdit ? (
        <>
          <Field
            id={inputId}
            label={t(recipe.image ? 'recipe_photo.replace' : 'recipe_photo.choose')}
          >
            <Input
              ref={input}
              id={inputId}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              disabled={busy || denied}
              onChange={handleSelect}
              aria-describedby={`${inputId}-limits${error ? ` ${inputId}-error` : ''}`}
              aria-invalid={!!error}
            />
          </Field>
          <p id={`${inputId}-limits`} className="text-muted-foreground text-sm">
            {t('recipe_photo.limits')}
          </p>
          {previewUrl ? (
            <img
              src={previewUrl}
              alt={t('recipe_photo.preview')}
              className="max-h-64 w-full rounded-lg object-contain"
            />
          ) : null}
          {error ? (
            <p id={`${inputId}-error`} role="alert" className="text-destructive text-sm">
              {error}
            </p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            {file ? (
              <>
                <Button
                  disabled={busy || denied}
                  onClick={() => {
                    handleChange(file);
                  }}
                >
                  {t(busy ? 'recipe_photo.saving' : 'recipe_photo.upload')}
                </Button>
                <Button variant="outline" disabled={busy} onClick={clearSelection}>
                  {t('common.cancel')}
                </Button>
              </>
            ) : null}
            {recipe.image ? (
              <Button
                variant="outline"
                disabled={busy || denied}
                onClick={() => {
                  handleChange(null);
                }}
              >
                {t(busy ? 'recipe_photo.saving' : 'recipe_photo.remove')}
              </Button>
            ) : null}
          </div>
        </>
      ) : null}
    </section>
  );
}
