import { Button, Card, MacroBar, Ring, StatusPill, type Macro } from '@shared/components/ui';
import { formatNumber } from '@shared/lib/utils';
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
  const pct = target > 0 ? (eaten / target) * 100 : 0;
  const over = eaten > target;

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
    <Card className="col-span-full flex flex-col gap-6 p-6 xl:col-span-2">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="text-[15px] font-bold">
            {over ? t('dashboard.hero_over_title') : t('dashboard.hero_on_track_title')}
          </div>
          <div className="text-muted-foreground text-[12.5px]">
            {t('dashboard.hero_progress', { percent: Math.round(pct) })}
          </div>
        </div>
        {over ? (
          <StatusPill variant="over" icon={TriangleAlert}>
            {t('dashboard.hero_over_budget')}
          </StatusPill>
        ) : (
          <StatusPill variant="good" icon={Check}>
            {t('dashboard.hero_within_budget')}
          </StatusPill>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-8">
        <Ring percent={pct} color={over ? 'fat' : 'primary'}>
          <div className="numeral text-[34px] leading-none font-bold">
            {formatNumber(Math.abs(remaining))}
          </div>
          <div className="text-muted-foreground mt-1 text-[11.5px] leading-tight">
            {over ? t('dashboard.hero_over_label') : t('dashboard.hero_left_label')}
          </div>
        </Ring>

        <div className="flex min-w-[260px] flex-1 flex-col gap-4">
          <div className="flex gap-6">
            <Stat label={t('dashboard.hero_eaten')} value={eaten} />
            <div className="bg-border w-px" />
            <Stat label={t('dashboard.hero_target')} value={target} />
            <div className="bg-border w-px" />
            <Stat label={t('dashboard.hero_remaining')} value={remaining} />
          </div>

          <div className="flex flex-col gap-3">
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
      </div>
    </Card>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <div className="text-muted-foreground text-[11px] font-semibold tracking-[0.06em] uppercase">
        {label}
      </div>
      <div className="numeral text-[19px] font-bold">{formatNumber(value)}</div>
    </div>
  );
}
