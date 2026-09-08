import { useHydrationConfig, useWaterIntake } from '@modules/diet-planner/api/hooks/useHydration';
import { Card, CardContent } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { Droplets } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { consumedNutrition } from '../../utils/consumedNutrition';
import { MacroProgressBar } from '../MacroProgressBar';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

interface Goals {
  dailyCalorieTarget: number | null;
  proteinGrams: number | null;
  carbsGrams: number | null;
  fatGrams: number | null;
  fiberGrams: number | null;
}

export interface DaySummaryCardProps {
  date: string;
  meals: MealEntryDto[];
  goals: Goals | null | undefined;
  className?: string;
}

const DEFAULT_DAILY_TARGET_ML = 2500;

export function DaySummaryCard({ date, meals, goals, className }: DaySummaryCardProps) {
  const { t } = useTranslation();
  const { data: hydrationConfig } = useHydrationConfig();
  const { data: hydration } = useWaterIntake(date);

  const totals = consumedNutrition(meals)[0] ?? {
    calories: 0,
    protein: 0,
    carbs: 0,
    fat: 0,
    fiber: 0,
  };

  const target = goals?.dailyCalorieTarget ?? null;
  const pct = target && target > 0 ? Math.min((totals.calories / target) * 100, 100) : null;

  const completed = meals.filter((m) => m.status === 'Done' || m.status === 'Modified').length;
  const total = meals.length;

  const waterMl = hydration?.totalMl ?? 0;
  const waterTarget = hydrationConfig?.dailyWaterTargetMl ?? DEFAULT_DAILY_TARGET_ML;
  const waterPct = waterTarget > 0 ? Math.min((waterMl / waterTarget) * 100, 100) : 0;

  return (
    <Card className={cn('lg:sticky lg:top-4', className)}>
      <CardContent className="space-y-6 p-5">
        {/* Calories */}
        <section className="text-center">
          <p className="text-muted-foreground text-[0.7rem] font-semibold tracking-widest uppercase">
            {t('dashboard.hero_eaten')}
          </p>
          <p className="mt-1 text-4xl font-bold tracking-tight tabular-nums">
            {Math.round(totals.calories)}
          </p>
          {target !== null && (
            <p className="text-muted-foreground mt-0.5 text-xs">
              {t('day_view.summary.of_target', { target: Math.round(target) })}
            </p>
          )}
          {pct !== null && (
            <div className="bg-muted mt-3 h-2 overflow-hidden rounded-full">
              <div
                className="h-full bg-gradient-to-r from-rose-500 to-orange-500 transition-all"
                style={{ width: `${String(pct)}%` }}
                data-testid="day-summary-calorie-bar"
              />
            </div>
          )}
        </section>

        {/* Macros */}
        <section className="space-y-3 border-t pt-4">
          <p className="text-muted-foreground text-[0.7rem] font-semibold tracking-widest uppercase">
            {t('day_view.summary.macros')}
          </p>
          <div className="space-y-2">
            <MacroProgressBar
              label={t('nutrition_summary.protein')}
              actual={totals.protein}
              goal={goals?.proteinGrams ?? null}
              gradient="from-blue-500 to-indigo-500"
            />
            <MacroProgressBar
              label={t('nutrition_summary.carbs')}
              actual={totals.carbs}
              goal={goals?.carbsGrams ?? null}
              gradient="from-emerald-500 to-teal-500"
            />
            <MacroProgressBar
              label={t('nutrition_summary.fat')}
              actual={totals.fat}
              goal={goals?.fatGrams ?? null}
              gradient="from-amber-500 to-orange-500"
            />
            <MacroProgressBar
              label={t('nutrition_summary.fiber')}
              actual={totals.fiber}
              goal={goals?.fiberGrams ?? null}
              gradient="from-violet-500 to-purple-500"
            />
          </div>
        </section>

        {/* Meals progress */}
        <section className="space-y-2 border-t pt-4">
          <div className="flex items-baseline justify-between">
            <p className="text-muted-foreground text-[0.7rem] font-semibold tracking-widest uppercase">
              {t('day_view.summary.meals')}
            </p>
            <span className="text-sm font-semibold tabular-nums">
              {completed} / {total}
            </span>
          </div>
          {total > 0 && (
            <div className="bg-muted h-2 overflow-hidden rounded-full">
              <div
                className="h-full bg-emerald-500 transition-all"
                style={{ width: `${String((completed / total) * 100)}%` }}
              />
            </div>
          )}
        </section>

        {/* Hydration */}
        <section className="space-y-2 border-t pt-4">
          <div className="flex items-baseline justify-between">
            <p className="text-muted-foreground text-[0.7rem] font-semibold tracking-widest uppercase">
              <Droplets className="mr-1 inline-block h-3 w-3" />
              {t('day_view.summary.hydration')}
            </p>
            <span className="text-sm font-semibold tabular-nums">
              {Math.round(waterMl)}
              <span className="text-muted-foreground ml-0.5 text-xs font-normal">
                /{Math.round(waterTarget)}ml
              </span>
            </span>
          </div>
          <div className="bg-muted h-2 overflow-hidden rounded-full">
            <div
              className="h-full bg-gradient-to-r from-cyan-500 to-blue-500 transition-all"
              style={{ width: `${String(waterPct)}%` }}
            />
          </div>
        </section>
      </CardContent>
    </Card>
  );
}
