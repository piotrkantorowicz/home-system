import { Button, Card, MacroBar, StatusPill, type Macro } from '@shared/components/ui';
import { cn, formatNumber } from '@shared/lib/utils';
import { Check, Target, TriangleAlert } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { Goal } from '@modules/diet-planner/api/hooks/useGoals';
import type { DailyNutrition } from '@modules/diet-planner/api/hooks/useMeals';

interface TodayHeroProps {
  goals: Goal | null;
  nutrition: DailyNutrition | undefined;
  onSetGoals: () => void;
}

interface MacroRow {
  key: Macro;
  label: string;
  value: number;
  target: number | null;
}

export function TodayHero({ goals, nutrition, onSetGoals }: TodayHeroProps) {
  const { t } = useTranslation();

  const target = goals?.dailyCalorieTarget ?? null;
  const eaten = Math.round(nutrition?.calories ?? 0);

  if (target === null) {
    return (
      <Card className="col-span-full flex flex-col items-center gap-4 p-8 text-center xl:col-span-2">
        <div className="bg-accent text-accent-foreground grid size-12 place-items-center rounded-2xl">
          <Target className="size-6" />
        </div>
        <div>
          <h3 className="text-[15px] font-bold">{t('dashboard.goals_cta_title')}</h3>
          <p className="text-muted-foreground mt-1 text-[12.5px]">
            {t('dashboard.goals_cta_description')}
          </p>
        </div>
        <Button size="xl" onClick={onSetGoals}>
          {t('dashboard.goals_cta_button')}
        </Button>
      </Card>
    );
  }

  const remaining = target - eaten;
  const over = eaten > target;
  const caloriePercent = target > 0 ? Math.min(100, Math.max(0, (eaten / target) * 100)) : 0;

  const macros: MacroRow[] = [
    {
      key: 'protein',
      label: t('dashboard.goal_protein'),
      value: Math.round(nutrition?.protein ?? 0),
      target: goals?.proteinGrams ?? null,
    },
    {
      key: 'carbs',
      label: t('dashboard.goal_carbs'),
      value: Math.round(nutrition?.carbs ?? 0),
      target: goals?.carbsGrams ?? null,
    },
    {
      key: 'fat',
      label: t('dashboard.goal_fat'),
      value: Math.round(nutrition?.fat ?? 0),
      target: goals?.fatGrams ?? null,
    },
    {
      key: 'fiber',
      label: t('dashboard.goal_fiber'),
      value: Math.round(nutrition?.fiber ?? 0),
      target: goals?.fiberGrams ?? null,
    },
  ];

  return (
    <Card className="col-span-full flex flex-col gap-5 p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">{t('dashboard.nutrition_title')}</h2>
        {!nutrition ? (
          <span className="text-text-2 text-sm">{t('dashboard.nothing_logged')}</span>
        ) : (
          <StatusPill variant={over ? 'over' : 'good'} icon={over ? TriangleAlert : Check}>
            {t(over ? 'dashboard.hero_over_budget' : 'dashboard.hero_within_budget')}
          </StatusPill>
        )}
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        <div className="min-w-0">
          <div className="flex flex-wrap items-baseline gap-3">
            <strong className="numeral text-5xl font-semibold">
              {formatNumber(Math.abs(remaining))}
            </strong>
            <span className="text-text-2 text-sm">
              {t(over ? 'dashboard.hero_over_label' : 'dashboard.hero_left_label')}
            </span>
          </div>
          <p className="text-text-2 mt-3 text-sm">
            {t('dashboard.hero_eaten')}: {formatNumber(eaten)} / {t('dashboard.hero_target')}:{' '}
            {formatNumber(target)}
          </p>
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
        </div>
        <div className="grid min-w-0 grid-cols-1 gap-4 sm:grid-cols-2">
          {macros.map((macro) =>
            macro.target === null ? null : (
              <MacroBar
                key={macro.key}
                macro={macro.key}
                label={macro.label}
                value={macro.value}
                target={macro.target}
              />
            ),
          )}
        </div>
      </div>
    </Card>
  );
}
