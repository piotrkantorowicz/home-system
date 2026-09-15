import { recipeOptions, useUpdateRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import { useToast } from '@shared/context/ToastContext';
import { useSuspenseQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { RecipeForm, type RecipeFormData } from '../../components/recipes/RecipeForm';

export default function RecipeEdit() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const { data: recipe } = useSuspenseQuery(recipeOptions(id ?? ''));
  const updateMutation = useUpdateRecipe(id ?? '');

  const handleSubmit = async (data: RecipeFormData) => {
    try {
      await updateMutation.mutateAsync({
        name: data.name,
        description: data.description ?? null,
        instructions: data.instructions ?? null,
        servings: data.servings,
        prepTimeMinutes: data.prepTimeMinutes ?? null,
        ingredients: data.ingredients.map((ing) => ({
          productId: ing.productId,
          amount: ing.amount,
          unit: ing.unit,
        })),
      });
      toast.success(t('recipe_form.update_success'));
      void navigate(`/diet-planner/recipes/${id ?? ''}`);
    } catch {
      toast.error(t('recipe_form.update_error'));
    }
  };

  if (!recipe) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-destructive text-lg">{t('recipe_detail.not_found')}</div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      <div className="mb-8">
        <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('recipe_form.edit_title')}</h1>
        <p className="text-muted-foreground text-[0.95rem]">
          {t('recipe_form.update_subtitle', { name: recipe.name })}
        </p>
      </div>

      <RecipeForm
        defaultValues={{
          name: recipe.name,
          description: recipe.description ?? '',
          instructions: recipe.instructions ?? '',
          servings: Number(recipe.servings),
          prepTimeMinutes: recipe.prepTimeMinutes ? Number(recipe.prepTimeMinutes) : undefined,
          ingredients: recipe.ingredients.map((ing) => ({
            productId: ing.productId,
            productName: ing.productName,
            amount: Number(ing.amount),
            unit: ing.unit,
          })),
        }}
        onSubmit={handleSubmit}
        isSubmitting={updateMutation.isPending}
        submitLabel={t('recipe_form.update_btn')}
      />
    </div>
  );
}
