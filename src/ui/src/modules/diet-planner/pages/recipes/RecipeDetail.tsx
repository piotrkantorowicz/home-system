import { useCreateMeal } from '@modules/diet-planner/api/hooks/useMeals';
import { recipeOptions, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import { MealForm } from '@modules/diet-planner/components/diet-plans/MealForm';
import { unitLabel } from '@modules/diet-planner/unitLabel';
import {
  Button,
  Card,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn, formatNumber } from '@shared/lib/utils';
import { useSuspenseQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { ChevronRight, Minus, Pencil, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';

const n = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

function toSteps(instructions: string): string[] {
  const byLine = instructions
    .split(/\r?\n/)
    .map((s) => s.trim().replace(/^\d+[.)]\s*/, ''))
    .filter(Boolean);
  if (byLine.length > 1) return byLine;
  return instructions
    .split(/(?<=\.)\s+(?=\d|[A-Z])/)
    .map((s) => s.trim().replace(/^\d+[.)]\s*/, ''))
    .filter(Boolean);
}

const MACROS = ['protein', 'carbs', 'fat', 'fiber'] as const;

export default function RecipeDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: recipe } = useSuspenseQuery(recipeOptions(id ?? ''));
  const deleteMutation = useDeleteRecipe();
  const createMeal = useCreateMeal();
  const toast = useToast();
  const [planOpen, setPlanOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [servings, setServings] = useState<number | null>(null);

  const baseServings = n(recipe?.servings) || 1;
  const shown = servings ?? baseServings;
  const ratio = shown / baseServings;

  const steps = recipe?.instructions ? toSteps(recipe.instructions) : [];

  if (!recipe) {
    return (
      <div className="px-4 py-6 md:px-8">
        <p className="text-destructive">{t('recipe_detail.not_found')}</p>
      </div>
    );
  }

  const per = recipe.nutritionPerServing;

  const handleAddToPlan = async (data: {
    date: string;
    mealSlotId: string;
    recipeId: string;
    servings: number;
    notes: string;
  }) => {
    try {
      await createMeal.mutateAsync({ ...data, mealTime: null, sequenceOrder: null });
      toast.success(t('meal_form.add_success'));
      setPlanOpen(false);
      void navigate(`/diet-planner/calendar?view=day&date=${data.date}`);
    } catch {
      toast.error(t('meal_form.add_error'));
    }
  };

  const handleDelete = async () => {
    if (!id) return;
    await deleteMutation.mutateAsync({ id });
    void navigate('/diet-planner/recipes');
  };

  return (
    <div className="animate-fade-in mx-auto flex max-w-6xl flex-col gap-5 px-4 py-6 md:px-8">
      <nav className="text-muted-foreground text-12-5px flex items-center gap-1">
        <Link to="/diet-planner/recipes" className="hover:text-foreground">
          {t('recipes.title')}
        </Link>
        <ChevronRight className="size-3.5" />
        <span className="text-text-2 font-semibold">{recipe.name}</span>
      </nav>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-26px font-bold">{recipe.name}</h1>
          <p className="text-muted-foreground mt-1 text-sm">
            {recipe.prepTimeMinutes
              ? `${String(n(recipe.prepTimeMinutes))} ${t('recipes.prep_time')} · `
              : ''}
            {t('recipes.servings', { count: shown })}
          </p>
        </div>
        <div className="flex flex-wrap gap-2.5">
          <Button
            size="xl"
            onClick={() => {
              setPlanOpen(true);
            }}
          >
            {t('recipe_detail.add_to_plan')}
          </Button>
          {recipe.isOwner ? (
            <>
              <Button size="xl" variant="outline" asChild>
                <Link to={`/diet-planner/recipes/${id ?? ''}/edit`}>
                  <Pencil className="size-4" />
                  {t('common.edit')}
                </Link>
              </Button>
              <Button
                size="xl"
                variant="outline"
                aria-label={t('common.delete')}
                onClick={() => {
                  setDeleteOpen(true);
                }}
              >
                <Trash2 className="text-destructive size-4" />
              </Button>
            </>
          ) : null}
        </div>
      </div>

      <div className="gap-18px grid items-start lg:grid-cols-3">
        <Card className="overflow-hidden p-0 lg:col-span-2">
          <div className="flex flex-col gap-6 p-6">
            {recipe.description ? (
              <p className="text-text-2 text-13px leading-relaxed">{recipe.description}</p>
            ) : null}

            <div>
              <div className="text-15px mb-2 font-bold">{t('recipe_detail.ingredients')}</div>
              <div className="border-border rounded-16px overflow-hidden border">
                <div className="bg-secondary text-muted-foreground text-10-5px grid grid-cols-[minmax(0,1fr)_auto] gap-4 px-3 py-2 font-semibold uppercase">
                  <span>{t('recipe_detail.table.product')}</span>
                  <span className="text-right">{t('recipe_detail.table.amount')}</span>
                </div>
                {recipe.ingredients.map((ing) => (
                  <div
                    key={ing.id}
                    className="border-border text-12-5px grid grid-cols-[minmax(0,1fr)_auto] gap-4 border-t px-3 py-2"
                  >
                    <span className="font-semibold break-words">{ing.productName}</span>
                    <span className="tnum text-right">
                      {(n(ing.amount) * ratio).toFixed(1)} {unitLabel(ing.unit, t)}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            {steps.length > 0 ? (
              <div>
                <div className="text-15px mb-2 font-bold">{t('recipe_detail.method')}</div>
                <ol className="flex flex-col gap-3">
                  {steps.map((step, i) => (
                    <li key={i} className="flex gap-3">
                      <span className="bg-accent text-accent-foreground text-12px grid size-[26px] flex-none place-items-center rounded-full font-bold">
                        {i + 1}
                      </span>
                      <p className="text-text-2 text-13-5px leading-relaxed text-pretty">{step}</p>
                    </li>
                  ))}
                </ol>
              </div>
            ) : null}
          </div>
        </Card>

        <div className="gap-18px flex flex-col">
          {per ? (
            <Card className="p-22px flex flex-col gap-3">
              <h2 className="text-15px font-bold">{t('recipe_detail.nutrition_per_serving')}</h2>
              <div className="numeral text-34px leading-none font-bold">
                {formatNumber(n(per.calories))}
                <span className="text-muted-foreground text-12px ml-1 font-medium">kcal</span>
              </div>
              <dl className="divide-border divide-y">
                {MACROS.map((m) => {
                  const grams = per[m];
                  return (
                    <div key={m} className="flex justify-between gap-4 py-2.5 text-sm">
                      <dt className="font-medium">{t(`nutrition_summary.${m}`)}</dt>
                      <dd className="text-text-2 tnum">{n(grams).toFixed(1)} g</dd>
                    </div>
                  );
                })}
              </dl>
              <p className="text-muted-foreground text-11px">
                {t('recipe_detail.per_serving_note')}
              </p>
            </Card>
          ) : null}

          <Card className="p-22px flex flex-col gap-3">
            <div className="text-15px font-bold">{t('recipe_detail.servings_label')}</div>
            <div className="flex items-center justify-between">
              <button
                type="button"
                aria-label={t('recipe_detail.decrease_servings')}
                onClick={() => {
                  setServings(Math.max(1, shown - 1));
                }}
                className={cn(
                  'border-border rounded-12px grid size-11 place-items-center border',
                  shown <= 1 && 'opacity-40',
                )}
                disabled={shown <= 1}
              >
                <Minus className="size-4" />
              </button>
              <span className="numeral text-22px font-bold">{shown}</span>
              <button
                type="button"
                aria-label={t('recipe_detail.increase_servings')}
                onClick={() => {
                  setServings(shown + 1);
                }}
                className="border-border rounded-12px grid size-11 place-items-center border"
              >
                <Plus className="size-4" />
              </button>
            </div>
            <p className="text-muted-foreground text-11px">{t('recipe_detail.scale_note')}</p>
            {per && (
              <p className="text-text-2 text-sm">
                {t('recipe_detail.total_calories', {
                  count: shown,
                  calories: formatNumber(n(per.calories) * shown),
                })}
              </p>
            )}
          </Card>
        </div>
      </div>

      {planOpen && (
        <MealForm
          open
          mode="create"
          onClose={() => {
            if (!createMeal.isPending) setPlanOpen(false);
          }}
          onSubmit={(data) => {
            void handleAddToPlan(data);
          }}
          isSubmitting={createMeal.isPending}
          initialValues={{
            recipeId: recipe.id,
            recipeName: recipe.name,
            servings: shown,
            notes: '',
            mealSlotId: '',
            date: format(new Date(), 'yyyy-MM-dd'),
          }}
        />
      )}

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('recipes.delete_dialog.title')}</DialogTitle>
            <DialogDescription>
              {t('recipes.delete_dialog.description', { name: recipe.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setDeleteOpen(false);
              }}
            >
              {t('common.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                void handleDelete();
              }}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending
                ? t('recipes.delete_dialog.deleting')
                : t('recipes.delete_dialog.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
