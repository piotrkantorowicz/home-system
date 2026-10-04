import { WaterSummary } from '@modules/diet-planner/components/dashboard/WaterSummary';
import { Button, Card, MacroBar, StatusPill, type Macro } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { usePreferences } from '@shared/hooks/usePreferences';
import { goalStatus, type GoalKind } from '@shared/lib/format';
import { cn } from '@shared/lib/utils';
import { Check, Target, TriangleAlert } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { Goal } from '@modules/diet-planner/api/hooks/useGoals';
import type { DailyNutrition } from '@modules/diet-planner/api/hooks/useMeals';

interface TodaySummaryProps {
  goals: Goal | null;
  nutrition: DailyNutrition | undefined;
  /** Local calendar day, `YYYY-MM-DD`, whose water is shown. */
  date: string;
  onSetGoals: () => void;
}

interface MacroRow {
  key: Macro;
  label: string;
  value: number;
  target: number | null;
  /** Fat and carbs are limits; protein and fibre are minimums, so exceeding them is not a warning. */
  kind: GoalKind;
}

/** Calories, macros and water for today on one surface. */
export function TodaySummary({ goals, nutrition, date, onSetGoals }: TodaySummaryProps) {
  const { t } = useTranslation();
  const fmt = useFormat();
  const { prefs } = usePreferences();

  const target = goals?.dailyCalorieTarget ?? null;
  const eaten = Math.round(nutrition?.calories ?? 0);
  const unit = prefs.energyUnit;
  // "1 410 kcal" → the figure gets the large type, the unit sits beside it.
  const figure = (kcal: number) => fmt.energy(kcal).slice(0, -unit.length).trim();

  const macros: MacroRow[] = [
    {
      key: 'protein',
      label: t('dashboard.goal_protein'),
      value: nutrition?.protein ?? 0,
      target: goals?.proteinGrams ?? null,
      kind: 'min',
    },
    {
      key: 'carbs',
      label: t('dashboard.goal_carbs'),
      value: nutrition?.carbs ?? 0,
      target: goals?.carbsGrams ?? null,
      kind: 'limit',
    },
    {
      key: 'fat',
      label: t('dashboard.goal_fat'),
      value: nutrition?.fat ?? 0,
      target: goals?.fatGrams ?? null,
      kind: 'limit',
    },
    {
      key: 'fiber',
      label: t('dashboard.goal_fiber'),
      value: nutrition?.fiber ?? 0,
      target: goals?.fiberGrams ?? null,
      kind: 'min',
    },
  ];

  const status = goalStatus(eaten, target, 'limit');
  const over = status.state === 'over';
  const left = (target ?? 0) - eaten;
  const caloriePercent =
    target !== null && target > 0 ? Math.min(100, Math.max(0, (eaten / target) * 100)) : 0;

  return (
    <Card className="grid gap-x-8 gap-y-6 p-5 sm:p-6 md:grid-cols-[repeat(auto-fit,minmax(260px,1fr))]">
      {target === null ? (
        <div className="flex min-w-0 flex-col items-start gap-3">
          <div className="bg-accent text-accent-foreground grid size-10 place-items-center rounded-2xl">
            <Target className="size-5" />
          </div>
          <div>
            <h2 className="text-15px font-bold">{t('dashboard.goals_cta_title')}</h2>
            <p className="text-muted-foreground text-12-5px mt-1">
              {t('dashboard.goals_cta_description')}
            </p>
          </div>
          <Button size="xl" onClick={onSetGoals}>
            {t('dashboard.goals_cta_button')}
          </Button>
        </div>
      ) : (
        <>
          <div className="min-w-0">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-label font-semibold">{t('dashboard.goal_calories')}</h2>
              {nutrition ? (
                <StatusPill variant={over ? 'over' : 'good'} icon={over ? TriangleAlert : Check}>
                  {t(over ? 'dashboard.hero_over_budget' : 'dashboard.hero_within_budget')}
                </StatusPill>
              ) : (
                <span className="text-text-2 text-sm">{t('dashboard.nothing_logged')}</span>
              )}
            </div>
            <div className="mt-3 flex flex-wrap items-baseline gap-x-3">
              <strong
                className={cn(
                  'numeral text-[44px] leading-none font-semibold',
                  over && 'text-destructive',
                )}
              >
                {figure(Math.abs(left))}
              </strong>
              <span className="text-text-2 text-sm">
                {unit} {t(left < 0 ? 'dashboard.hero_over_unit' : 'dashboard.hero_left_unit')}
              </span>
            </div>
            <div
              role="progressbar"
              aria-label={t('dashboard.hero_eaten')}
              aria-valuemin={0}
              aria-valuemax={target}
              aria-valuenow={Math.min(eaten, target)}
              className="bg-muted mt-4 h-2 overflow-hidden rounded-full"
            >
              <div
                className={cn('h-full rounded-full', over ? 'bg-fat' : 'bg-primary')}
                style={{ width: `${String(caloriePercent)}%` }}
              />
            </div>
            <p className="text-text-2 mt-2 flex justify-between gap-3 text-sm">
              <span>{t('dashboard.hero_eaten_amount', { amount: figure(eaten) })}</span>
              <span>{t('dashboard.hero_target_amount', { amount: fmt.energy(target) })}</span>
            </p>
          </div>
          <div className="flex min-w-0 flex-col gap-3">
            <h2 className="text-label font-semibold">{t('dashboard.macros_title')}</h2>
            {macros.map((macro) =>
              macro.target === null ? null : (
                <MacroBar
                  key={macro.key}
                  macro={macro.key}
                  label={macro.label}
                  value={macro.value}
                  target={macro.target}
                  valueText={`${fmt.grams(macro.value)} / ${fmt.grams(macro.target)} g`}
                  showOverflow={macro.kind === 'limit'}
                />
              ),
            )}
          </div>
        </>
      )}
      <WaterSummary date={date} />
    </Card>
  );
}
