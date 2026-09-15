import {
  useCompleteMeal,
  useResetMeal,
  type MealEntryDto,
} from '@modules/diet-planner/api/hooks/useMeals';
import { isConsumed } from '@modules/diet-planner/utils/consumedNutrition';
import { Button, Card, Skeleton } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn, formatNumber } from '@shared/lib/utils';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

interface NextUpCardProps {
  meals: MealEntryDto[];
  loading: boolean;
  onAddMeal: () => void;
}

const UNDO_WINDOW_MS = 6000;

function mealTime(meal: MealEntryDto): string {
  return (meal.mealTime ?? meal.mealSlotDefaultTime).slice(0, 5);
}

export function NextUpCard({ meals, loading, onAddMeal }: NextUpCardProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const complete = useCompleteMeal();
  const reset = useResetMeal();
  const pendingIds = useRef(new Set<string>());
  const [pending, setPending] = useState(new Set<string>());
  const latestMeals = useRef(meals);
  useEffect(() => {
    latestMeals.current = meals;
  });
  const heading = useRef<HTMLHeadingElement>(null);
  const ordered = [...meals].sort(
    (a, b) =>
      mealTime(a).localeCompare(mealTime(b)) ||
      Number(a.mealSlotSortOrder) - Number(b.mealSlotSortOrder),
  );
  const upcoming = ordered.filter((meal) => !isConsumed(meal));
  const logged = ordered.filter(isConsumed);

  const markEaten = async (meal: MealEntryDto) => {
    if (pendingIds.current.has(meal.id)) return;
    pendingIds.current.add(meal.id);
    setPending(new Set(pendingIds.current));
    let completed = false;
    try {
      await complete.mutateAsync(meal.id);
      completed = true;
    } catch {
      toast.error(t('dashboard.meal_mark_error'));
    }
    pendingIds.current.delete(meal.id);
    setPending(new Set(pendingIds.current));
    if (!completed) return;

    let available = true;
    const closeUndoWindow = setTimeout(() => {
      available = false;
    }, UNDO_WINDOW_MS);
    toast.success(t('dashboard.meal_marked_eaten'), {
      duration: UNDO_WINDOW_MS,
      action: {
        label: t('hydration.undo'),
        onClick: () => {
          const current = latestMeals.current.find((entry) => entry.id === meal.id);
          if (
            !available ||
            current?.status !== 'Done' ||
            current.recipeId !== meal.recipeId ||
            current.servings !== meal.servings
          )
            return;
          available = false;
          clearTimeout(closeUndoWindow);
          reset.mutate(meal.id, {
            onError: () => {
              toast.error(t('calendar.meal_action_error.reset'));
            },
          });
        },
      },
    });
    heading.current?.focus();
  };

  return (
    <Card className="flex min-w-0 flex-col gap-5 p-5 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 ref={heading} tabIndex={-1} className="text-lg font-semibold">
          {t('dashboard.next_up_title')}
        </h2>
        <Link
          to="/diet-planner/calendar"
          className="text-primary inline-flex min-h-11 items-center text-sm font-semibold"
        >
          {t('dashboard.full_plan')}
        </Link>
      </div>
      {loading ? (
        <Skeleton className="h-40 w-full" />
      ) : ordered.length === 0 ? (
        <div className="flex flex-col items-start gap-4">
          <p className="text-text-2 text-sm">{t('dashboard.no_meals')}</p>
          <Button variant="outline" onClick={onAddMeal}>
            {t('dashboard.log_meal')}
          </Button>
        </div>
      ) : (
        <>
          {upcoming.length === 0 && (
            <p className="text-text-2 text-sm">{t('dashboard.all_logged')}</p>
          )}
          {upcoming.map((meal, index) => (
            <article
              key={meal.id}
              className={cn(
                'grid min-w-0 grid-cols-[3rem_minmax(0,1fr)] gap-3 border-t py-5',
                index === 0 && 'bg-accent border-primary/40 rounded-2xl border p-4',
              )}
            >
              <time className="text-text-2 text-sm">{mealTime(meal)}</time>
              <div className="min-w-0">
                <p className="text-text-2 text-sm">{meal.mealSlotName}</p>
                <h3 className="mt-1 text-base font-semibold break-words">{meal.recipeName}</h3>
                <p className="text-text-2 mt-2 text-sm">
                  {t('dashboard.meal_macros', {
                    kcal: formatNumber(Number(meal.calories)),
                    protein: Math.round(Number(meal.protein)),
                    carbs: Math.round(Number(meal.carbs)),
                    fat: Math.round(Number(meal.fat)),
                  })}
                </p>
                <Button
                  className="mt-4"
                  variant={index === 0 ? 'default' : 'outline'}
                  disabled={pending.has(meal.id)}
                  onClick={() => {
                    void markEaten(meal);
                  }}
                >
                  {t('dashboard.mark_eaten')}
                </Button>
              </div>
            </article>
          ))}
          {logged.length > 0 && (
            <details className="border-t">
              <summary className="text-text-2 min-h-11 cursor-pointer py-3 text-sm">
                {t('dashboard.logged_meals', { count: logged.length })}
              </summary>
              {logged.map((meal) => (
                <article key={meal.id} className="border-t py-4">
                  <p className="text-text-2 text-sm">
                    {mealTime(meal)} · {meal.mealSlotName}
                  </p>
                  <h3 className="font-semibold break-words">
                    {meal.actualRecipe?.name ?? meal.recipeName}
                  </h3>
                  <p className="text-text-2 text-sm">
                    {t('dashboard.kcal_logged', { kcal: formatNumber(Number(meal.calories)) })}
                  </p>
                </article>
              ))}
            </details>
          )}
        </>
      )}
    </Card>
  );
}
