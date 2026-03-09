import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@shared/components/ui/Dialog';
import { Button } from '@shared/components/ui/Button';
import { Input } from '@shared/components/ui/Input';
import { Label } from '@shared/components/ui/Label';
import { useRecipes } from '@modules/diet-planner/api/hooks/useRecipes';

const MEAL_TYPES = ['breakfast', 'lunch', 'dinner', 'snack'] as const;

interface MealFormData {
  date: string;
  mealType: string;
  recipeId: string;
  recipeName: string;
  servings: number;
  notes: string;
}

interface MealFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: Omit<MealFormData, 'recipeName'>) => void;
  initialDate?: string;
  initialMealType?: string;
  initialValues?: {
    recipeId: string;
    recipeName: string;
    servings: number;
    notes: string;
    mealType: string;
    date: string;
  };
  isSubmitting?: boolean;
  mode: 'create' | 'edit';
}

export function MealForm({
  open,
  onClose,
  onSubmit,
  initialDate,
  initialMealType,
  initialValues,
  isSubmitting,
  mode,
}: MealFormProps) {
  const { t } = useTranslation('diet-planner');
  const [recipeSearch, setRecipeSearch] = useState(initialValues?.recipeName ?? '');
  const [showRecipeList, setShowRecipeList] = useState(false);
  const [form, setForm] = useState<MealFormData>({
    date: initialValues?.date ?? initialDate ?? '',
    mealType: initialValues?.mealType ?? initialMealType ?? 'breakfast',
    recipeId: initialValues?.recipeId ?? '',
    recipeName: initialValues?.recipeName ?? '',
    servings: initialValues?.servings ?? 1,
    notes: initialValues?.notes ?? '',
  });

  const { data: recipesData } = useRecipes({ search: recipeSearch, pageSize: 20 });
  const recipes =
    (recipesData as { items?: { id: string; name: string }[] } | undefined)?.items ?? [];

  const handleRecipeSelect = (id: string, name: string) => {
    setForm((f) => ({ ...f, recipeId: id, recipeName: name }));
    setRecipeSearch(name);
    setShowRecipeList(false);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.recipeId || !form.date || !form.mealType) return;
    onSubmit({
      date: form.date,
      mealType: form.mealType,
      recipeId: form.recipeId,
      servings: form.servings,
      notes: form.notes,
    });
  };

  const isValid = !!form.recipeId && !!form.date && !!form.mealType && form.servings > 0;

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>
            {mode === 'create' ? t('meal_form.add_title') : t('meal_form.edit_title')}
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="meal-date">{t('meal_form.date_label')}</Label>
              <Input
                id="meal-date"
                type="date"
                value={form.date}
                onChange={(e) => setForm((f) => ({ ...f, date: e.target.value }))}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="meal-type">{t('meal_form.meal_type_label')}</Label>
              <select
                id="meal-type"
                value={form.mealType}
                onChange={(e) => setForm((f) => ({ ...f, mealType: e.target.value }))}
                className="flex h-9 w-full rounded-md border px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1"
                style={{
                  backgroundColor: 'hsl(var(--color-background))',
                  borderColor: 'hsl(var(--color-input))',
                  color: 'hsl(var(--color-foreground))',
                }}
              >
                {MEAL_TYPES.map((mt) => (
                  <option key={mt} value={mt}>
                    {t(`diet_plan_detail.meal_types.${mt}`)}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="space-y-1.5 relative">
            <Label htmlFor="recipe-search">{t('meal_form.recipe_label')}</Label>
            <Input
              id="recipe-search"
              placeholder={t('meal_form.recipe_placeholder')}
              value={recipeSearch}
              onChange={(e) => {
                setRecipeSearch(e.target.value);
                setForm((f) => ({ ...f, recipeId: '', recipeName: '' }));
                setShowRecipeList(true);
              }}
              onFocus={() => setShowRecipeList(true)}
              autoComplete="off"
            />
            {showRecipeList && recipeSearch && recipes.length > 0 && (
              <div
                className="absolute z-10 w-full mt-1 rounded-md border shadow-lg overflow-hidden"
                style={{
                  backgroundColor: 'hsl(var(--color-card))',
                  borderColor: 'hsl(var(--color-border))',
                }}
              >
                {recipes.map((r) => (
                  <button
                    key={r.id}
                    type="button"
                    className="w-full text-left px-3 py-2 text-sm hover:bg-accent/50 transition-colors"
                    onMouseDown={() => handleRecipeSelect(r.id, r.name)}
                  >
                    {r.name}
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="servings">{t('meal_form.servings_label')}</Label>
            <Input
              id="servings"
              type="number"
              min="0.1"
              max="50"
              step="0.5"
              value={form.servings}
              onChange={(e) =>
                setForm((f) => ({ ...f, servings: parseFloat(e.target.value) || 1 }))
              }
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="notes">{t('meal_form.notes_label')}</Label>
            <Input
              id="notes"
              placeholder={t('meal_form.notes_placeholder')}
              value={form.notes}
              onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={!isValid || isSubmitting}>
              {isSubmitting
                ? t('common.saving')
                : mode === 'create'
                  ? t('meal_form.add_btn')
                  : t('meal_form.save_btn')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
