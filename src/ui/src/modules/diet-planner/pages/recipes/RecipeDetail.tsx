import { useRecipe, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
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
  Skeleton,
} from '@shared/components/ui';
import { cn, formatNumber } from '@shared/lib/utils';
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
  const { data: recipe, isLoading, error } = useRecipe(id ?? '');
  const deleteMutation = useDeleteRecipe();
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [servings, setServings] = useState<number | null>(null);

  const baseServings = n(recipe?.servings) || 1;
  const shown = servings ?? baseServings;
  const ratio = shown / baseServings;

  const steps = recipe?.instructions ? toSteps(recipe.instructions) : [];

  if (isLoading) {
    return (
      <div className="mx-auto max-w-6xl px-4 py-6 md:px-8">
        <Skeleton className="h-[420px] w-full rounded-[22px]" />
      </div>
    );
  }
  if (error || !recipe) {
    return (
      <div className="px-4 py-6 md:px-8">
        <p className="text-destructive">{t('recipe_detail.not_found')}</p>
      </div>
    );
  }

  const per = recipe.nutritionPerServing;
  const perScaled = per
    ? {
        calories: n(per.calories) * ratio,
        protein: n(per.protein) * ratio,
        carbs: n(per.carbs) * ratio,
        fat: n(per.fat) * ratio,
        fiber: n(per.fiber) * ratio,
      }
    : null;

  const handleDelete = async () => {
    if (!id) return;
    await deleteMutation.mutateAsync({ id });
    void navigate('/diet-planner/recipes');
  };

  return (
    <div className="animate-fade-in mx-auto flex max-w-6xl flex-col gap-5 px-4 py-6 md:px-8">
      <nav className="text-muted-foreground flex items-center gap-1 text-[12.5px]">
        <Link to="/diet-planner/recipes" className="hover:text-foreground">
          {t('recipes.title')}
        </Link>
        <ChevronRight className="size-3.5" />
        <span className="text-text-2 font-semibold">{recipe.name}</span>
      </nav>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold">{recipe.name}</h1>
          <p className="text-muted-foreground mt-1 text-sm">
            {recipe.prepTimeMinutes
              ? `${String(n(recipe.prepTimeMinutes))} ${t('recipes.prep_time')} · `
              : ''}
            {t('recipes.servings', { count: baseServings })}
          </p>
        </div>
        {recipe.isOwner ? (
          <div className="flex flex-wrap gap-2.5">
            <Button size="xl" asChild>
              <Link to="/diet-planner/calendar">{t('recipe_detail.add_to_plan')}</Link>
            </Button>
            <Button size="xl" variant="outline" asChild>
              <Link to={`/diet-planner/recipes/${id ?? ''}/edit`}>
                <Pencil className="size-4" />
                {t('common.edit')}
              </Link>
            </Button>
            <Button
              size="xl"
              variant="outline"
              onClick={() => {
                setDeleteOpen(true);
              }}
            >
              <Trash2 className="text-destructive size-4" />
            </Button>
          </div>
        ) : null}
      </div>

      <div className="grid gap-[18px] lg:grid-cols-3">
        <Card className="overflow-hidden p-0 lg:col-span-2">
          <div
            className="h-[180px]"
            style={{
              background:
                'linear-gradient(140deg, color-mix(in oklab, var(--color-primary) 40%, transparent), color-mix(in oklab, var(--color-water) 20%, transparent))',
            }}
          />
          <div className="flex flex-col gap-6 p-6">
            {recipe.description ? (
              <p className="text-text-2 text-[13px] leading-relaxed">{recipe.description}</p>
            ) : null}

            <div>
              <div className="mb-2 text-[15px] font-bold">{t('recipe_detail.ingredients')}</div>
              <div className="border-border overflow-hidden rounded-[16px] border">
                <div className="bg-secondary text-muted-foreground grid grid-cols-[2fr_0.7fr_0.7fr] gap-2 px-3 py-2 text-[10.5px] font-semibold uppercase">
                  <span>{t('recipe_detail.table.product')}</span>
                  <span className="text-right">{t('recipe_detail.table.amount')}</span>
                  <span className="text-right">kcal</span>
                </div>
                {recipe.ingredients.map((ing) => (
                  <div
                    key={ing.id}
                    className="border-border grid grid-cols-[2fr_0.7fr_0.7fr] gap-2 border-t px-3 py-2 text-[12.5px]"
                  >
                    <span className="truncate font-semibold">{ing.productName}</span>
                    <span className="tnum text-right">
                      {(n(ing.amount) * ratio).toFixed(1)} {unitLabel(ing.unit, t)}
                    </span>
                    <span className="text-text-2 tnum text-right">—</span>
                  </div>
                ))}
              </div>
            </div>

            {steps.length > 0 ? (
              <div>
                <div className="mb-2 text-[15px] font-bold">{t('recipe_detail.method')}</div>
                <ol className="flex flex-col gap-3">
                  {steps.map((step, i) => (
                    <li key={i} className="flex gap-3">
                      <span className="bg-accent text-accent-foreground grid size-[26px] flex-none place-items-center rounded-full text-[12px] font-bold">
                        {i + 1}
                      </span>
                      <p className="text-text-2 text-[13.5px] leading-relaxed text-pretty">
                        {step}
                      </p>
                    </li>
                  ))}
                </ol>
              </div>
            ) : null}
          </div>
        </Card>

        <div className="flex flex-col gap-[18px]">
          {perScaled ? (
            <Card className="flex flex-col gap-3 p-[22px]">
              <div className="text-[15px] font-bold">
                {t('recipe_detail.nutrition_per_serving')}
              </div>
              <div className="numeral text-[34px] leading-none font-bold">
                {formatNumber(perScaled.calories)}
                <span className="text-muted-foreground ml-1 text-[12px] font-medium">kcal</span>
              </div>
              <div className="flex flex-col gap-2.5">
                {MACROS.map((m) => {
                  const grams = perScaled[m];
                  const pct = Math.min(100, (grams / Math.max(perScaled.calories / 4, 1)) * 100);
                  return (
                    <div key={m} className="flex flex-col gap-1">
                      <div className="flex justify-between text-[12px]">
                        <span className="font-semibold capitalize">{m}</span>
                        <span className="text-text-2 tnum">{grams.toFixed(1)} g</span>
                      </div>
                      <div className="bg-muted h-1.5 overflow-hidden rounded-full">
                        <div
                          className="h-full rounded-full"
                          style={{ width: `${String(pct)}%`, background: `var(--color-${m})` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
              <p className="text-muted-foreground text-[11px]">
                {t('recipe_detail.per_serving_note')}
              </p>
            </Card>
          ) : null}

          <Card className="flex flex-col gap-3 p-[22px]">
            <div className="text-[15px] font-bold">{t('recipe_detail.servings_label')}</div>
            <div className="flex items-center justify-between">
              <button
                type="button"
                aria-label="decrease"
                onClick={() => {
                  setServings(Math.max(1, shown - 1));
                }}
                className={cn(
                  'border-border grid size-10 place-items-center rounded-[12px] border',
                  shown <= 1 && 'opacity-40',
                )}
                disabled={shown <= 1}
              >
                <Minus className="size-4" />
              </button>
              <span className="numeral text-[22px] font-bold">{shown}</span>
              <button
                type="button"
                aria-label="increase"
                onClick={() => {
                  setServings(shown + 1);
                }}
                className="border-border grid size-10 place-items-center rounded-[12px] border"
              >
                <Plus className="size-4" />
              </button>
            </div>
            <p className="text-muted-foreground text-[11px]">{t('recipe_detail.scale_note')}</p>
          </Card>
        </div>
      </div>

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
