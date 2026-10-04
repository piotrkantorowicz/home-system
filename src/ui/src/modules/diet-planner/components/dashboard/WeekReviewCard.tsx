import { buildWeekHistory } from '@modules/diet-planner/utils/weekHistory';
import { Card, DailyBars, MetricTile, type DailyBar } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { usePreferences } from '@shared/hooks/usePreferences';
import { useTranslation } from 'react-i18next';

import type { DailyNutrition } from '@modules/diet-planner/api/hooks/useMeals';

interface WeekReviewCardProps {
  /** Consumed nutrition per day (any order, gaps = nothing logged). */
  week: DailyNutrition[];
  target: number | null;
  /** Local calendar day, `YYYY-MM-DD`; the last of the seven bars. */
  today: string;
}

/** Last 7 days of calories against the daily target, with full-day averages. */
export function WeekReviewCard({ week, target, today }: WeekReviewCardProps) {
  const { t, i18n } = useTranslation();
  const fmt = useFormat();
  const { prefs } = usePreferences();
  const unit = prefs.energyUnit;
  const figure = (kcal: number) => fmt.energy(kcal).slice(0, -unit.length).trim();

  const history = buildWeekHistory(week, today, target);
  const nothingLogged = history.days.every((day) => day.calories === null);

  const bars: DailyBar[] = history.days.map((day) => {
    const date = new Date(`${day.date}T00:00:00`);
    return {
      key: day.date,
      label: date.toLocaleDateString(i18n.language, { weekday: 'short' }),
      sublabel: date.toLocaleDateString(i18n.language, { day: 'numeric', month: 'numeric' }),
      value: day.calories,
      text: day.calories === null ? '' : figure(day.calories),
      over: day.over,
      isToday: day.isToday,
      srText: `${date.toLocaleDateString(i18n.language, { weekday: 'long', day: 'numeric', month: 'long' })}: ${
        day.calories === null ? t('dashboard.nothing_logged') : fmt.energy(day.calories)
      }`,
    };
  });

  return (
    <Card className="flex min-w-0 flex-col gap-5 p-6">
      <div className="text-15px font-bold">{t('dashboard.week_review_title')}</div>
      {nothingLogged ? (
        <p className="text-text-2 text-sm">{t('dashboard.week_empty')}</p>
      ) : (
        <>
          <DailyBars
            days={bars}
            target={target}
            {...(target === null
              ? {}
              : { targetLabel: t('dashboard.hero_target_amount', { amount: fmt.energy(target) }) })}
            ariaLabel={t('dashboard.week_review_title')}
            overLabel={t('dashboard.week_over_marker')}
            missingText="—"
          />
          <p className="text-text-2 text-sm">{t('dashboard.week_partial')}</p>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <MetricTile
              label={t('dashboard.week_avg_full_days')}
              value={history.avgCalories === null ? '—' : figure(history.avgCalories)}
              {...(history.avgCalories === null ? {} : { hint: unit })}
            />
            <MetricTile
              label={t('dashboard.week_over_days')}
              value={target === null || history.fullDays === 0 ? '—' : String(history.overDays)}
              accent={history.overDays > 0 ? 'fat' : 'default'}
            />
            <MetricTile
              label={t('dashboard.week_avg_protein')}
              value={history.avgProtein === null ? '—' : fmt.grams(history.avgProtein)}
              {...(history.avgProtein === null ? {} : { hint: 'g' })}
            />
          </div>
        </>
      )}
    </Card>
  );
}
