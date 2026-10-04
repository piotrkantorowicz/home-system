import { goalsOptions } from '@modules/diet-planner/api/hooks/useGoals';
import { useCreateMeal, useMeals } from '@modules/diet-planner/api/hooks/useMeals';
import { productOptions } from '@modules/diet-planner/api/hooks/useProducts';
import { recipeOptions, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import { MealForm } from '@modules/diet-planner/components/diet-plans/MealForm';
import { mealAccess } from '@modules/diet-planner/utils/householdAccess';
import { macroEnergyShares } from '@modules/diet-planner/utils/macroEnergyShares';
import { toVisibility } from '@modules/diet-planner/utils/visibility';
import { useHousehold } from '@modules/household';
import {
  Button,
  Card,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  PageContainer,
  PageHeader,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { usePreferences } from '@shared/hooks/usePreferences';
import { cn } from '@shared/lib/utils';
import { useQueries, useQuery, useSuspenseQuery } from '@tanstack/react-query';
import { endOfWeek, format, startOfWeek } from 'date-fns';
import { Ellipsis, Minus, Pencil, Plus, Trash2 } from 'lucide-react';
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
  const fmt = useFormat();
  const { prefs } = usePreferences();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: recipe } = useSuspenseQuery(recipeOptions(id ?? ''));
  const deleteMutation = useDeleteRecipe();
  const createMeal = useCreateMeal();
  const toast = useToast();
  const { myRole, myPersonId, members } = useHousehold();
  const [planOpen, setPlanOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [servings, setServings] = useState<number | null>(null);

  // The meals endpoint is bounded by date: read the whole current week, then keep this recipe.
  const weekOpts = { weekStartsOn: prefs.weekStart === 'sunday' ? 0 : 1 } as const;
  const today = new Date();
  const weekFrom = format(startOfWeek(today, weekOpts), 'yyyy-MM-dd');
  const weekTo = format(endOfWeek(today, weekOpts), 'yyyy-MM-dd');
  const { data: weekMeals } = useMeals({ from: weekFrom, to: weekTo });
  const { data: goals } = useQuery(goalsOptions());

  // A product link is offered only when the caller can actually open the product (404 = not visible).
  const productChecks = useQueries({
    queries: (recipe?.ingredients ?? []).map((ing) => productOptions(ing.productId)),
  });

  const baseServings = n(recipe?.servings) || 1;
  const shown = servings ?? baseServings;
  const ratio = shown / baseServings;

  const steps = recipe?.instructions ? toSteps(recipe.instructions) : [];

  if (!recipe) {
    return (
      <PageContainer>
        <p className="text-destructive">{t('recipe_detail.not_found')}</p>
      </PageContainer>
    );
  }

  const per = recipe.nutritionPerServing;
  const shares = macroEnergyShares(per);
  const kcalTarget = goals?.dailyCalorieTarget ?? null;
  const planned = (weekMeals ?? [])
    .filter((m) => m.recipeId === recipe.id)
    .sort(
      (a, b) => a.date.localeCompare(b.date) || n(a.mealSlotSortOrder) - n(b.mealSlotSortOrder),
    );
  const canPlan = mealAccess(myRole, myPersonId ?? undefined, undefined).canPlan;
  // The recipe only carries an auth subject, never a name: name the author only when it is the caller.
  const author = recipe.isOwner
    ? members.find((m) => m.personId === myPersonId)?.displayName
    : undefined;
  const prep = n(recipe.prepTimeMinutes);
  const meta = [
    prep > 0 ? t('recipes.prep_minutes', { count: prep }) : null,
    t(`visibility.${toVisibility(recipe.visibility)}`),
    author ? t('recipe_detail.by', { name: author }) : null,
  ]
    .filter((x): x is string => x !== null)
    .join(' · ');

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
    <PageContainer>
      <PageHeader
        title={recipe.name}
        subtitle={meta}
        breadcrumb={[
          { label: t('recipes.title'), href: '/diet-planner/recipes' },
          { label: recipe.name },
        ]}
        actions={
          <>
            {recipe.canEdit ? (
              <Button variant="outline" asChild>
                <Link to={`/diet-planner/recipes/${id ?? ''}/edit`}>
                  <Pencil className="size-4" />
                  {t('common.edit')}
                </Link>
              </Button>
            ) : null}
            {recipe.canEdit ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="icon" aria-label={t('common.actions')}>
                    <Ellipsis className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onSelect={() => {
                      setDeleteOpen(true);
                    }}
                  >
                    <Trash2 />
                    {t('common.delete')}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : null}
            {canPlan ? (
              <Button
                onClick={() => {
                  setPlanOpen(true);
                }}
              >
                {t('recipe_detail.add_to_plan')}
              </Button>
            ) : null}
          </>
        }
      />

      <div className="grid items-start gap-5 lg:grid-cols-3">
        <Card className="overflow-hidden p-0 lg:col-span-2">
          <div className="flex flex-col gap-6 p-6">
            {recipe.description ? (
              <p className="text-text-2 text-13px leading-relaxed">{recipe.description}</p>
            ) : null}

            <section>
              <div className="mb-2 flex items-center justify-between gap-3">
                <h2 className="text-15px font-bold">{t('recipe_detail.ingredients')}</h2>
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    aria-label={t('recipe_detail.decrease_servings')}
                    onClick={() => {
                      setServings(Math.max(1, shown - 1));
                    }}
                    className={cn(
                      'border-border grid size-11 place-items-center rounded-full border',
                      shown <= 1 && 'opacity-40',
                    )}
                    disabled={shown <= 1}
                  >
                    <Minus className="size-4" />
                  </button>
                  <span
                    className="tnum min-w-20 text-center text-sm font-semibold"
                    aria-live="polite"
                  >
                    {t('recipes.servings', { count: shown })}
                  </span>
                  <button
                    type="button"
                    aria-label={t('recipe_detail.increase_servings')}
                    onClick={() => {
                      setServings(shown + 1);
                    }}
                    className="border-border grid size-11 place-items-center rounded-full border"
                  >
                    <Plus className="size-4" />
                  </button>
                </div>
              </div>
              <ul className="border-border divide-border divide-y overflow-hidden rounded-2xl border">
                {recipe.ingredients.map((ing, i) => (
                  <li
                    key={ing.id}
                    className="text-13px flex items-center justify-between gap-4 px-3 py-2.5"
                  >
                    {productChecks[i]?.data ? (
                      <Link
                        to={`/diet-planner/products/${ing.productId}`}
                        className="text-primary font-semibold break-words underline-offset-2 hover:underline"
                      >
                        {ing.productName}
                      </Link>
                    ) : (
                      <span className="font-semibold break-words">{ing.productName}</span>
                    )}
                    <span className="tnum text-text-2 shrink-0">
                      {fmt.quantity(n(ing.amount) * ratio, ing.unit)}
                    </span>
                  </li>
                ))}
              </ul>
            </section>

            {steps.length > 0 ? (
              <section>
                <h2 className="text-15px mb-2 font-bold">{t('recipe_detail.method')}</h2>
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
              </section>
            ) : null}
          </div>
        </Card>

        <div className="flex flex-col gap-5">
          {per ? (
            <Card className="flex flex-col gap-3 p-5">
              <h2 className="text-15px font-bold">{t('recipe_detail.nutrition_per_serving')}</h2>
              <div className="tnum text-[40px] leading-none font-bold">
                {fmt.energy(n(per.calories))}
              </div>
              {kcalTarget && kcalTarget > 0 ? (
                <p className="text-muted-foreground text-sm">
                  {t('recipe_detail.of_your_day', {
                    percent: Math.round((n(per.calories) / kcalTarget) * 100),
                  })}
                </p>
              ) : null}
              {shares ? (
                <div
                  role="img"
                  aria-label={t('recipes.macro_bar_aria', {
                    protein: Math.round(shares.protein),
                    carbs: Math.round(shares.carbs),
                    fat: Math.round(shares.fat),
                  })}
                  className="bg-muted flex h-2 overflow-hidden rounded-full"
                >
                  <span className="bg-protein" style={{ width: `${String(shares.protein)}%` }} />
                  <span className="bg-carbs" style={{ width: `${String(shares.carbs)}%` }} />
                  <span className="bg-fat" style={{ width: `${String(shares.fat)}%` }} />
                </div>
              ) : null}
              <dl className="divide-border divide-y">
                {MACROS.map((m) => (
                  <div key={m} className="flex justify-between gap-4 py-2.5 text-sm">
                    <dt className="font-medium">{t(`nutrition_summary.${m}`)}</dt>
                    <dd className="text-text-2 tnum">{fmt.grams(n(per[m]))}</dd>
                  </div>
                ))}
              </dl>
              {shown > 1 ? (
                <p className="text-muted-foreground text-sm">
                  {t('recipe_detail.all_servings', {
                    count: shown,
                    energy: fmt.energy(n(per.calories) * shown),
                  })}
                </p>
              ) : null}
            </Card>
          ) : null}

          {planned.length > 0 ? (
            <Card className="flex flex-col gap-2 p-5" data-testid="recipe-plan-context">
              <h2 className="text-15px font-bold">{t('recipe_detail.in_your_plan')}</h2>
              <ul className="divide-border divide-y">
                {planned.map((m) => (
                  <li key={m.id}>
                    <Link
                      to={`/diet-planner/calendar?view=day&date=${m.date}`}
                      className="hover:text-primary flex justify-between gap-3 py-2.5 text-sm"
                    >
                      <span className="font-medium">{fmt.dayShort(m.date)}</span>
                      <span className="text-muted-foreground">{m.mealSlotName}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            </Card>
          ) : null}
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
    </PageContainer>
  );
}
