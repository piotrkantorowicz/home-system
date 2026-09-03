import { useCompleteMeal, type MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';
import { Button, Card, Skeleton } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn, formatNumber } from '@shared/lib/utils';
import { ArrowRight, Check, Plus, UtensilsCrossed } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

interface NextUpCardProps {
  meals: MealEntryDto[];
  loading: boolean;
  onAddMeal: () => void;
}

const num = (v: number | string): number => (typeof v === 'number' ? v : Number(v));

function mealTime(meal: MealEntryDto): string {
  return (meal.mealTime ?? meal.mealSlotDefaultTime).slice(0, 5);
}

function isDone(meal: MealEntryDto): boolean {
  return meal.status === 'Done' || meal.status === 'Modified';
}

function sortMeals(a: MealEntryDto, b: MealEntryDto): number {
  const t = mealTime(a).localeCompare(mealTime(b));
  return t !== 0 ? t : num(a.mealSlotSortOrder) - num(b.mealSlotSortOrder);
}

export function NextUpCard({ meals, loading, onAddMeal }: NextUpCardProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const completeMeal = useCompleteMeal();

  const ordered = [...meals].sort(sortMeals);
  const featured = ordered.find((m) => !isDone(m));
  const rest = ordered.filter((m) => m.id !== featured?.id);

  const markEaten = async (id: string) => {
    try {
      await completeMeal.mutateAsync(id);
      toast.success(t('dashboard.meal_marked_eaten'));
    } catch {
      toast.error(t('dashboard.meal_mark_error'));
    }
  };

  return (
    <Card className="flex flex-col gap-4 p-[22px]">
      <div className="flex items-center justify-between">
        <div className="text-[15px] font-bold">{t('dashboard.next_up_title')}</div>
        <Link
          to="/diet-planner/calendar"
          className="text-primary inline-flex items-center gap-1 text-[12.5px] font-semibold"
        >
          {t('dashboard.full_plan')}
          <ArrowRight className="size-3.5" />
        </Link>
      </div>

      {loading ? (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-24 w-full rounded-[16px]" />
          <Skeleton className="h-10 w-full" />
        </div>
      ) : ordered.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-6 text-center">
          <div className="bg-muted grid size-11 place-items-center rounded-full">
            <UtensilsCrossed className="text-muted-foreground size-5" />
          </div>
          <p className="text-muted-foreground text-[12.5px]">{t('dashboard.no_meals')}</p>
          <Button size="xs" variant="outline" onClick={onAddMeal}>
            <Plus className="size-4" />
            {t('dashboard.log_meal')}
          </Button>
        </div>
      ) : (
        <>
          {featured ? (
            <div className="border-accent-foreground/20 bg-accent flex gap-3.5 rounded-[16px] border p-3.5">
              <div className="text-accent-foreground w-11 flex-none text-center">
                <div className="text-[15px] leading-tight font-bold">{mealTime(featured)}</div>
                <div className="text-[10px] font-semibold opacity-75">{featured.mealSlotName}</div>
              </div>
              <div className="min-w-0 flex-1">
                <div className="truncate text-[14px] font-semibold">{featured.recipeName}</div>
                <div className="text-text-2 tnum mt-0.5 text-[12px]">
                  {t('dashboard.meal_macros', {
                    kcal: formatNumber(num(featured.calories)),
                    protein: Math.round(num(featured.protein)),
                    carbs: Math.round(num(featured.carbs)),
                    fat: Math.round(num(featured.fat)),
                  })}
                </div>
                <div className="mt-2.5">
                  <Button
                    size="chip"
                    onClick={() => {
                      void markEaten(featured.id);
                    }}
                    disabled={completeMeal.isPending}
                  >
                    <Check className="size-3.5" />
                    {t('dashboard.mark_eaten')}
                  </Button>
                </div>
              </div>
            </div>
          ) : null}

          <div className="flex flex-col">
            {rest.map((meal) => {
              const done = isDone(meal);
              return (
                <div
                  key={meal.id}
                  className={cn(
                    'border-border flex items-center gap-3.5 border-t py-2.5',
                    done && 'opacity-60',
                  )}
                >
                  <div className="text-muted-foreground w-11 flex-none text-center">
                    <div className="text-[13.5px] leading-tight font-semibold">
                      {mealTime(meal)}
                    </div>
                    <div className="text-[10px] font-semibold">{meal.mealSlotName}</div>
                  </div>
                  <div className="min-w-0 flex-1">
                    <div
                      className={cn('truncate text-[13.5px] font-semibold', done && 'line-through')}
                    >
                      {meal.recipeName}
                    </div>
                    <div className="text-muted-foreground text-[11.5px]">
                      {done
                        ? t('dashboard.kcal_logged', { kcal: formatNumber(num(meal.calories)) })
                        : t('dashboard.kcal', { kcal: formatNumber(num(meal.calories)) })}
                    </div>
                  </div>
                  {!done ? (
                    <Button
                      size="xs"
                      variant="ghost"
                      onClick={() => {
                        void markEaten(meal.id);
                      }}
                      disabled={completeMeal.isPending}
                      aria-label={t('dashboard.mark_eaten')}
                    >
                      <Check className="size-4" />
                    </Button>
                  ) : null}
                </div>
              );
            })}
          </div>
        </>
      )}
    </Card>
  );
}
