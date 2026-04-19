import { useRecipes } from '@modules/diet-planner/api/hooks/useRecipes';
import { Button } from '@shared/components/ui/Button';
import { DatePicker } from '@shared/components/ui/DatePicker';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@shared/components/ui/Dialog';
import { Input } from '@shared/components/ui/Input';
import { Label } from '@shared/components/ui/Label';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

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
  initialDate?: string | undefined;
  initialMealType?: string | undefined;
  initialValues?:
    | {
        recipeId: string;
        recipeName: string;
        servings: number;
        notes: string;
        mealType: string;
        date: string;
      }
    | undefined;
  isSubmitting?: boolean | undefined;
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
  const { t } = useTranslation();
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

  const handleSubmit = (e: React.SyntheticEvent<HTMLFormElement>) => {
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
    <Dialog
      open={open}
      onOpenChange={(v) => {
        if (!v) onClose();
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>
            {mode === 'create' ? t('meal_form.add_title') : t('meal_form.edit_title')}
          </DialogTitle>
          <DialogDescription className="sr-only">
            {mode === 'create' ? t('meal_form.add_title') : t('meal_form.edit_title')}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>{t('meal_form.date_label')}</Label>
              <DatePicker
                value={form.date || null}
                onChange={(v) => {
                  setForm((f) => ({ ...f, date: v ?? '' }));
                }}
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="meal-type">{t('meal_form.meal_type_label')}</Label>
              <select
                id="meal-type"
                value={form.mealType}
                onChange={(e) => {
                  setForm((f) => ({ ...f, mealType: e.target.value }));
                }}
                className="border-input bg-background text-foreground flex h-9 w-full rounded-md border px-3 py-1 text-sm shadow-sm transition-colors focus-visible:ring-1 focus-visible:outline-none"
              >
                {MEAL_TYPES.map((mt) => (
                  <option key={mt} value={mt}>
                    {t(`diet_plan_detail.meal_types.${mt}`)}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="relative space-y-1.5">
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
              onFocus={() => {
                setShowRecipeList(true);
              }}
              autoComplete="off"
            />
            {showRecipeList && recipeSearch && recipes.length > 0 && (
              <div className="border-border bg-card absolute z-10 mt-1 w-full overflow-hidden rounded-md border shadow-lg">
                {recipes.map((r) => (
                  <button
                    key={r.id}
                    type="button"
                    className="hover:bg-accent/50 w-full px-3 py-2 text-left text-sm transition-colors"
                    onMouseDown={() => {
                      handleRecipeSelect(r.id, r.name);
                    }}
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
              min="0.5"
              max="50"
              step="0.5"
              value={form.servings}
              onChange={(e) => {
                setForm((f) => ({ ...f, servings: parseFloat(e.target.value) || 1 }));
              }}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="notes">{t('meal_form.notes_label')}</Label>
            <Input
              id="notes"
              placeholder={t('meal_form.notes_placeholder')}
              value={form.notes}
              onChange={(e) => {
                setForm((f) => ({ ...f, notes: e.target.value }));
              }}
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
