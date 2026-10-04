import { Card } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { goalStatus, type GoalKind } from '@shared/lib/format';
import { cn } from '@shared/lib/utils';
import { ArrowRight, ArrowUp, Check, Target } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import type { Goal } from '@modules/diet-planner/api/hooks/useGoals';

export interface WeekTotals {
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  fiber: number;
}

interface WeekMetersProps {
  /** Everything planned for the week, eaten or not. */
  totals: WeekTotals;
  /** `null` while no goals are set; the card then offers to set them. */
  goals: Goal | null;
}

interface Meter {
  key: keyof WeekTotals;
  label: string;
  fill: string;
  kind: GoalKind;
  daily: number | null;
}

const GRAMS = new Set<keyof WeekTotals>(['protein', 'carbs', 'fat', 'fiber']);

/** The five weekly meters with a target tick and a direction-aware status line. */
export function WeekMeters({ totals, goals }: WeekMetersProps) {
  const { t } = useTranslation();
  const fmt = useFormat();

  const meters: Meter[] = [
    {
      key: 'calories',
      label: t('nutrition_summary.calories'),
      fill: 'bg-primary',
      kind: 'limit',
      daily: goals?.dailyCalorieTarget ?? null,
    },
    {
      key: 'protein',
      label: t('nutrition_summary.protein'),
      fill: 'bg-protein',
      kind: 'min',
      daily: goals?.proteinGrams ?? null,
    },
    {
      key: 'carbs',
      label: t('nutrition_summary.carbs'),
      fill: 'bg-carbs',
      kind: 'limit',
      daily: goals?.carbsGrams ?? null,
    },
    {
      key: 'fat',
      label: t('nutrition_summary.fat'),
      fill: 'bg-fat',
      kind: 'limit',
      daily: goals?.fatGrams ?? null,
    },
    {
      key: 'fiber',
      label: t('nutrition_summary.fiber'),
      fill: 'bg-fiber',
      kind: 'min',
      daily: goals?.fiberGrams ?? null,
    },
  ];
  const amount = (key: keyof WeekTotals, value: number) =>
    GRAMS.has(key) ? `${fmt.grams(value)} g` : fmt.energy(value);

  return (
    <Card className="mt-6 p-5 sm:p-6">
      <div className="mb-4 flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="text-lg font-semibold">{t('calendar.week_meters_title')}</h2>
        {goals ? (
          <Link
            to="/diet-planner/profile?section=goals"
            className="text-primary inline-flex min-h-11 items-center text-sm font-semibold"
          >
            {t('nutrition_summary.goals_edit')}
          </Link>
        ) : null}
      </div>
      {goals === null ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed py-8 text-center">
          <Target className="text-muted-foreground size-5" />
          <p className="text-muted-foreground text-sm">{t('nutrition_summary.goals_cta_title')}</p>
          <Link
            to="/diet-planner/profile?section=goals"
            className="bg-primary text-primary-foreground inline-flex min-h-11 items-center gap-2 rounded-lg px-4 text-sm font-medium"
          >
            {t('nutrition_summary.goals_cta_button')}
            <ArrowRight className="size-4" />
          </Link>
        </div>
      ) : (
        <div className="grid gap-x-6 gap-y-5 sm:grid-cols-2 lg:grid-cols-5">
          {meters.map((meter) => {
            const value = totals[meter.key];
            if (meter.daily === null)
              return (
                <div key={meter.key} className="min-w-0">
                  <div className="text-label font-semibold">{meter.label}</div>
                  <p className="text-text-2 mt-1 text-sm">{amount(meter.key, value)}</p>
                </div>
              );
            const target = meter.daily * 7;
            const status = goalStatus(value, target, meter.kind);
            // The scale leaves room past the target so an overshoot is visible beside the tick.
            const scale = Math.max(target, value) * 1.1 || 1;
            return (
              <div key={meter.key} className="min-w-0">
                <div className="text-label flex justify-between gap-2">
                  <span className="font-semibold">{meter.label}</span>
                  <span className="text-text-2 tnum">
                    {amount(meter.key, value)} / {amount(meter.key, target)}
                  </span>
                </div>
                <div className="bg-muted relative mt-1.5 h-2 overflow-hidden rounded-full">
                  <div
                    className={cn('h-full rounded-full', meter.fill)}
                    style={{ width: `${String(Math.min(100, (value / scale) * 100))}%` }}
                  />
                  <span
                    aria-hidden="true"
                    className="bg-foreground absolute inset-y-0 w-0.5"
                    style={{ left: `${String((target / scale) * 100)}%` }}
                  />
                </div>
                <p
                  className={cn(
                    'text-label mt-1.5 inline-flex items-center gap-1',
                    status.state === 'over' && 'text-over font-semibold',
                    (status.state === 'onTarget' || status.state === 'met') && 'text-good',
                    (status.state === 'under' || status.state === 'short') && 'text-text-2',
                  )}
                >
                  {status.state === 'over' && <ArrowUp className="size-3" strokeWidth={3} />}
                  {(status.state === 'onTarget' || status.state === 'met') && (
                    <Check className="size-3" strokeWidth={3} />
                  )}
                  {status.state === 'none'
                    ? null
                    : t(`calendar.week_status.${status.state}`, {
                        amount: amount(meter.key, Math.abs(status.diff)),
                      })}
                </p>
              </div>
            );
          })}
        </div>
      )}
    </Card>
  );
}
