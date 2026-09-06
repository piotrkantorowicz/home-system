import { Card, MetricTile } from '@shared/components/ui';
import { cn, formatNumber } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

import type { DailyNutrition } from '@modules/diet-planner/api/hooks/useMeals';

interface WeekReviewCardProps {
  /** Last 7 days of nutrition summaries (any order, may have gaps). */
  week: DailyNutrition[];
  target: number | null;
}

function last7Dates(): string[] {
  const out: string[] = [];
  const base = new Date();
  for (let i = 6; i >= 0; i--) {
    const d = new Date(base);
    d.setDate(base.getDate() - i);
    out.push(
      `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
        d.getDate(),
      ).padStart(2, '0')}`,
    );
  }
  return out;
}

export function WeekReviewCard({ week, target }: WeekReviewCardProps) {
  const { t } = useTranslation();
  const dates = last7Dates();
  const byDate = new Map(week.map((d) => [d.date.slice(0, 10), d]));

  const days = dates.map((date, index) => ({
    date,
    calories: Math.round(byDate.get(date)?.calories ?? 0),
    isToday: index === dates.length - 1,
    weekday: new Date(`${date}T00:00:00`).toLocaleDateString(undefined, { weekday: 'short' }),
  }));

  const logged = days.filter((d) => d.calories > 0);
  const avg =
    logged.length > 0 ? Math.round(logged.reduce((s, d) => s + d.calories, 0) / logged.length) : 0;
  const scaleMax = Math.max(target ?? 0, ...days.map((d) => d.calories), 1);

  return (
    <Card className="col-span-full flex flex-col gap-5 p-[22px] xl:col-span-2">
      <div className="text-[15px] font-bold">{t('dashboard.week_review_title')}</div>

      <div className="grid grid-cols-3 gap-3">
        <MetricTile label={t('dashboard.week_avg_intake')} value={formatNumber(avg)} hint="kcal" />
        <MetricTile
          label={t('dashboard.week_days_logged')}
          value={`${String(logged.length)} / 7`}
        />
        <MetricTile
          label={t('dashboard.hero_target')}
          value={target === null ? '—' : formatNumber(target)}
          {...(target === null ? {} : { hint: 'kcal' })}
        />
      </div>

      <div className="flex h-[132px] items-end gap-2.5 border-b pb-0">
        {days.map((day) => {
          const heightPct = day.calories > 0 ? Math.max(6, (day.calories / scaleMax) * 100) : 30;
          return (
            <div key={day.date} className="flex flex-1 flex-col items-center gap-1.5">
              <div className="flex w-full flex-1 items-end justify-center">
                <div
                  className={cn(
                    'w-full max-w-[46px] rounded-t-[10px] rounded-b-[3px]',
                    day.calories === 0 && 'border-border-strong border border-dashed',
                  )}
                  style={{
                    height: `${String(heightPct)}%`,
                    background:
                      day.calories === 0
                        ? 'transparent'
                        : day.isToday
                          ? 'var(--color-primary)'
                          : 'color-mix(in oklab, var(--color-primary) 32%, transparent)',
                  }}
                />
              </div>
              <span
                className={cn(
                  'text-[10.5px]',
                  day.isToday ? 'text-foreground font-bold' : 'text-muted-foreground',
                )}
              >
                {day.weekday}
              </span>
            </div>
          );
        })}
      </div>
    </Card>
  );
}
