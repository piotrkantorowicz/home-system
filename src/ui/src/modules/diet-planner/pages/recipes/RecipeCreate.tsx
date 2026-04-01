import { api } from '@modules/diet-planner/api/client';
import { useCreateRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { RecipeForm, type RecipeFormData } from '../../components/recipes/RecipeForm';

export default function RecipeCreate() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createMutation = useCreateRecipe();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleSubmit = async (data: RecipeFormData) => {
    setSubmitError(null);
    try {
      const ingredients = await Promise.all(
        data.ingredients.map(async (ing) => {
          if (ing.productId) {
            return { productId: ing.productId, amount: ing.amount, unit: ing.unit };
          }
          // Resolve product name → ID
          const response = await api.GET('/api/v1/products', {
            params: { query: { Search: ing.productName, PageSize: 10 } },
          });
          const items = (
            response.data as { items: { id: string; name: string }[] } | undefined
          )?.items ?? [];
          const product = items.find((p) => p.name === ing.productName);
          if (!product) throw new Error(`Product "${ing.productName}" not found`);
          return { productId: product.id, amount: ing.amount, unit: ing.unit };
        }),
      );

      await createMutation.mutateAsync({
        name: data.name,
        description: data.description ?? null,
        instructions: data.instructions ?? null,
        servings: data.servings,
        prepTimeMinutes: data.prepTimeMinutes ?? null,
        ingredients,
      });
      void navigate('/diet-planner/recipes');
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      console.error('Failed to create recipe:', error);
      setSubmitError(msg);
    }
  };

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      <div className="mb-8">
        <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('recipe_form.create_title')}</h1>
        <p className="text-muted-foreground text-[0.95rem]">{t('recipe_form.create_subtitle')}</p>
      </div>

      {submitError && (
        <p role="alert" className="text-destructive mb-4 text-sm">
          {submitError}
        </p>
      )}

      <RecipeForm
        onSubmit={handleSubmit}
        isSubmitting={createMutation.isPending}
        submitLabel={t('recipe_form.create_btn')}
      />
    </div>
  );
}
