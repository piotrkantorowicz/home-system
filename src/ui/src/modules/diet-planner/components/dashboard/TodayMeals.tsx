import {
  useCompleteMeal,
  useResetMeal,
  type MealEntryDto,
} from '@modules/diet-planner/api/hooks/useMeals';
import { isConsumed } from '@modules/diet-planner/utils/consumedNutrition';
import { Button, Card, Skeleton } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { goalStatus } from '@shared/lib/format';
import { cn } from '@shared/lib/utils';
import { Check } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

interface TodayMealsProps {
  meals: MealEntryDto[];
  /** Daily calorie target; `null` when no goal is set. */
  target: number | null;
  loading: boolean;
  onAddMeal: () => void;
}

const UNDO_WINDOW_MS = 6000;

function mealTime(meal: MealEntryDto): string {
  return (meal.mealTime ?? meal.mealSlotDefaultTime).slice(0, 5);
}

/** Every slot of today as a row with a check toggle; the next uneaten meal is emphasised. Never struck through. */
export function TodayMeals({ meals, target, loading, onAddMeal }: TodayMealsProps) {
  const { t } = useTranslation();
  const fmt = useFormat();
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
  const next = ordered.find((meal) => !isConsumed(meal));
  const eatenCount = ordered.filter(isConsumed).length;
  const planned = ordered.reduce((sum, meal) => sum + Number(meal.calories), 0);
  const plannedStatus = goalStatus(planned, target, 'limit');

  const markEaten = async (meal: MealEntryDto, refocus: boolean) => {
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
    // The primary button disappears once its meal is eaten; keep keyboard focus on the list.
    if (refocus) heading.current?.focus();
  };

  const unmark = (meal: MealEntryDto) => {
    if (pendingIds.current.has(meal.id)) return;
    pendingIds.current.add(meal.id);
    setPending(new Set(pendingIds.current));
    reset.mutate(meal.id, {
      onError: () => {
        toast.error(t('calendar.meal_action_error.reset'));
      },
      onSettled: () => {
        pendingIds.current.delete(meal.id);
        setPending(new Set(pendingIds.current));
      },
    });
  };

  return (
    <Card className="flex min-w-0 flex-col gap-3 p-5 sm:p-6">
      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
        <h2 ref={heading} tabIndex={-1} className="text-lg font-semibold">
          {t('dashboard.meals_title')}
        </h2>
        {ordered.length > 0 && (
          <span className="text-text-2 text-sm">
            {t('dashboard.meals_eaten_count', { eaten: eatenCount, total: ordered.length })}
          </span>
        )}
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
          <ul>
            {ordered.map((meal) => {
              const consumed = isConsumed(meal);
              const isNext = meal.id === next?.id;
              const name = meal.actualRecipe?.name ?? meal.recipeName;
              const modified = meal.status === 'Modified';
              return (
                <li
                  key={meal.id}
                  className={cn(
                    'grid min-w-0 grid-cols-[3rem_minmax(0,1fr)_auto] items-center gap-x-3 border-t py-2.5 first:border-t-0',
                    isNext && 'bg-accent -mx-2 rounded-xl border-t-0 px-2',
                  )}
                >
                  <time
                    className={cn('text-text-2 text-sm', isNext && 'text-primary font-semibold')}
                  >
                    {mealTime(meal)}
                  </time>
                  <div className="min-w-0">
                    <p
                      className={cn('text-text-2 text-xs', isNext && 'text-primary font-semibold')}
                    >
                      {isNext
                        ? t('dashboard.meal_slot_next', { slot: meal.mealSlotName })
                        : meal.mealSlotName}
                    </p>
                    <h3 className="text-sm font-semibold break-words">{name}</h3>
                    {isNext && (
                      <p className="text-text-2 mt-0.5 text-xs">
                        {t('dashboard.meal_macros', {
                          kcal: fmt.energy(Number(meal.calories)),
                          protein: fmt.grams(Number(meal.protein)),
                          carbs: fmt.grams(Number(meal.carbs)),
                          fat: fmt.grams(Number(meal.fat)),
                        })}
                      </p>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    {isNext ? (
                      <Button
                        className="max-sm:hidden"
                        disabled={pending.has(meal.id)}
                        onClick={() => {
                          void markEaten(meal, true);
                        }}
                      >
                        {t('dashboard.mark_eaten')}
                      </Button>
                    ) : null}
                    <span className={cn('numeral text-text-2 text-sm', isNext && 'sm:hidden')}>
                      {fmt.energy(Number(meal.calories))}
                    </span>
                    <button
                      type="button"
                      aria-pressed={consumed}
                      aria-label={t(consumed ? 'dashboard.meal_unmark' : 'dashboard.meal_mark', {
                        name,
                      })}
                      title={modified ? t('dashboard.meal_modified_hint') : undefined}
                      disabled={pending.has(meal.id) || modified}
                      onClick={() => {
                        if (consumed) unmark(meal);
                        else void markEaten(meal, false);
                      }}
                      className="focus-visible:ring-ring grid size-11 shrink-0 place-items-center rounded-full focus-visible:ring-2 focus-visible:outline-none disabled:opacity-60"
                    >
                      <span
                        className={cn(
                          'grid size-7 place-items-center rounded-full border-2',
                          consumed
                            ? 'bg-primary border-primary text-primary-foreground'
                            : isNext
                              ? 'border-primary'
                              : 'border-input',
                        )}
                      >
                        {consumed && <Check className="size-4" strokeWidth={3} />}
                      </span>
                    </button>
                  </div>
                </li>
              );
            })}
          </ul>
          <p className="text-text-2 border-t pt-3 text-sm">
            {target === null || plannedStatus.state === 'none'
              ? t('dashboard.planned_footer', { kcal: fmt.energy(planned) })
              : t('dashboard.planned_footer_vs_target', {
                  kcal: fmt.energy(planned),
                  status: t(`dashboard.planned_${plannedStatus.state}`, {
                    kcal: fmt.energy(Math.abs(plannedStatus.diff)),
                  }),
                })}
          </p>
        </>
      )}
    </Card>
  );
}
