import { useCreateRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { RecipeForm, type RecipeFormData } from '../../components/recipes/RecipeForm';

export default function RecipeCreate() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createMutation = useCreateRecipe();

  const handleSubmit = async (data: RecipeFormData) => {
    try {
      await createMutation.mutateAsync({
        name: data.name,
        description: data.description ?? null,
        instructions: data.instructions ?? null,
        servings: data.servings,
        prepTimeMinutes: data.prepTimeMinutes ?? null,
        ingredients: data.ingredients.map((ing) => ({
          productName: ing.productName,
          amount: ing.amount,
          unit: ing.unit,
        })),
      });
      void navigate('/diet-planner/recipes');
    } catch (error) {
      console.error('Failed to create recipe:', error);
    }
  };

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      <div className="mb-8">
        <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('recipe_form.create_title')}</h1>
        <p className="text-muted-foreground text-[0.95rem]">{t('recipe_form.create_subtitle')}</p>
      </div>

      <RecipeForm
        onSubmit={handleSubmit}
        isSubmitting={createMutation.isPending}
        submitLabel={t('recipe_form.create_btn')}
      />
    </div>
  );
}
