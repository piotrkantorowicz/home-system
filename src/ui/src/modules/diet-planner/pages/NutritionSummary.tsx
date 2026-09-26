import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import { useNutritionSummary } from '@modules/diet-planner/api/hooks/useMeals';
import {
  Banner,
  Card,
  EmptyState,
  MetricTile,
  Pagination,
  SegmentedControl,
  Skeleton,
} from '@shared/components/ui';
import { cn, formatNumber, formatSigned } from '@shared/lib/utils';
import { CalendarX } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

type Range = '7' | '30' | '90';

function toDateStr(date: Date): string {
  return `${String(date.getFullYear())}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(
    date.getDate(),
  ).padStart(2, '0')}`;
}

const DAY_MS = 86400000;

function rangeBounds(range: Range): { from: string; to: string } {
  const now = new Date();
  const start = new Date(now.getTime() - (Number(range) - 1) * DAY_MS);
  return { from: toDateStr(start), to: toDateStr(now) };
}

function lastDates(range: Range): string[] {
  const out: string[] = [];
  const now = new Date();
  for (let i = Number(range) - 1; i >= 0; i--) {
    out.push(toDateStr(new Date(now.getTime() - i * DAY_MS)));
  }
  return out;
}

export default function NutritionSummary() {
  const { t } = useTranslation();
  const [range, setRange] = useState<Range>('7');
  const [tablePage, setTablePage] = useState(1);
  const [tablePageSize, setTablePageSize] = useState(25);

  const { from, to } = rangeBounds(range);

  const { data, isLoading, isError, refetch } = useNutritionSummary({ from, to });
  const { data: goals } = useGoals();

  const days = data ?? [];
  const target = goals?.dailyCalorieTarget ?? null;

  const logged = days.filter((d) => d.calories > 0);
  const avg = (pick: (d: (typeof days)[number]) => number): number =>
    logged.length > 0 ? logged.reduce((s, d) => s + pick(d), 0) / logged.length : 0;

  const avgKcal = avg((d) => d.calories);
  const avgProtein = avg((d) => d.protein);
  const overDays = target !== null ? days.filter((d) => d.calories > target).length : 0;

  const dateList = lastDates(range);
  const byDate = new Map(days.map((d) => [d.date.slice(0, 10), d]));
  const todayStr = toDateStr(new Date());

  const scaleMax = Math.max(target ?? 0, ...days.map((d) => d.calories), 1);
  const targetLinePct = target !== null ? (target / scaleMax) * 100 : null;

  const macroSplit = (p: number, c: number, f: number) => {
    const total = p * 4 + c * 4 + f * 9 || 1;
    return {
      protein: Math.round(((p * 4) / total) * 100),
      carbs: Math.round(((c * 4) / total) * 100),
      fat: Math.round(((f * 9) / total) * 100),
    };
  };
  const actualSplit = macroSplit(
    avgProtein,
    avg((d) => d.carbs),
    avg((d) => d.fat),
  );
  const targetSplit = goals
    ? macroSplit(goals.proteinGrams ?? 0, goals.carbsGrams ?? 0, goals.fatGrams ?? 0)
    : null;

  const pageStart = (tablePage - 1) * tablePageSize;
  const pagedDays = days.slice(pageStart, pageStart + tablePageSize);

  return (
    <div className="animate-fade-in mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-26px font-bold">{t('nutrition_page.title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">{t('nutrition_page.subtitle')}</p>
        </div>
        <SegmentedControl
          label={t('nutrition_page.range_label')}
          value={range}
          onChange={setRange}
          options={[
            { value: '7', label: t('nutrition_page.range_7') },
            { value: '30', label: t('nutrition_page.range_30') },
            { value: '90', label: t('nutrition_page.range_90') },
          ]}
        />
      </div>

      {isError ? (
        <Banner
          variant="error"
          onRetry={() => {
            void refetch();
          }}
          retryLabel={t('dashboard.retry')}
        >
          {t('dashboard.data_error')}
        </Banner>
      ) : isLoading ? (
        <Skeleton className="h-420px rounded-22px w-full" />
      ) : days.length === 0 ? (
        <EmptyState
          icon={CalendarX}
          title={t('nutrition_page.no_data')}
          description={t('nutrition_page.no_data_desc')}
        />
      ) : (
        <>
          <div className="gap-18px grid grid-cols-2 lg:grid-cols-4">
            <MetricTile
              label={t('nutrition_page.avg_intake')}
              value={formatNumber(avgKcal)}
              hint={target !== null ? formatSigned(avgKcal - target) : 'kcal'}
              {...(target !== null && avgKcal > target ? { accent: 'fat' as const } : {})}
            />
            <MetricTile
              label={t('nutrition_page.avg_protein')}
              value={`${avgProtein.toFixed(0)} g`}
            />
            <MetricTile
              label={t('nutrition_page.days_logged')}
              value={`${String(logged.length)} / ${String(days.length)}`}
            />
            <MetricTile
              label={t('nutrition_page.over_days')}
              value={String(overDays)}
              {...(overDays > 0 ? { accent: 'fat' as const } : {})}
            />
          </div>

          <Card className="p-22px flex flex-col gap-4">
            <div className="text-15px font-bold">{t('nutrition_page.intake_vs_target')}</div>
            <div className="relative flex h-[190px] items-end gap-1">
              {targetLinePct !== null ? (
                <div
                  className="border-fat pointer-events-none absolute inset-x-0 border-t-2 border-dashed"
                  style={{ bottom: `${String(targetLinePct)}%` }}
                />
              ) : null}
              {dateList.map((date) => {
                const day = byDate.get(date);
                const kcal = day?.calories ?? 0;
                const isToday = date === todayStr;
                const over = target !== null && kcal > target;
                const h = kcal > 0 ? Math.max(4, (kcal / scaleMax) * 100) : 22;
                return (
                  <div
                    key={date}
                    role="img"
                    aria-label={t('nutrition_page.chart_bar_label', {
                      date,
                      kcal: Math.round(kcal),
                    })}
                    className={cn(
                      'flex-1 rounded-t-[5px]',
                      kcal === 0 && 'border-border-strong border border-dashed',
                    )}
                    style={{
                      height: `${String(h)}%`,
                      background:
                        kcal === 0
                          ? 'transparent'
                          : isToday
                            ? 'var(--color-primary)'
                            : over
                              ? 'color-mix(in oklab, var(--color-fat) 45%, transparent)'
                              : 'color-mix(in oklab, var(--color-primary) 30%, transparent)',
                    }}
                  />
                );
              })}
            </div>
          </Card>

          <Card className="p-22px flex flex-col gap-4">
            <div className="text-15px font-bold">{t('nutrition_page.macro_split')}</div>
            <SplitRow label={t('nutrition_page.actual')} split={actualSplit} />
            {targetSplit ? (
              <SplitRow label={t('nutrition_page.target')} split={targetSplit} dim />
            ) : null}
            <div className="text-muted-foreground text-11px flex gap-4">
              {(['protein', 'carbs', 'fat'] as const).map((m) => (
                <span key={m} className="inline-flex items-center gap-1.5 capitalize">
                  <span
                    className="size-2 rounded-full"
                    style={{ background: `var(--color-${m})` }}
                  />
                  {t(`products.table.${m}`)}
                </span>
              ))}
            </div>
          </Card>

          <Card className="overflow-x-auto p-0" role="table">
            <div className="min-w-[560px]">
              <div
                role="row"
                className="bg-secondary text-muted-foreground text-10-5px grid grid-cols-[1.4fr_1fr_0.8fr_0.8fr_0.8fr_0.8fr] gap-2 px-4 py-2.5 font-semibold uppercase"
              >
                <span role="columnheader">{t('nutrition_page.date')}</span>
                <span role="columnheader" className="text-right">
                  kcal
                </span>
                <span role="columnheader" className="text-right">
                  {t('nutrition_summary.protein')}
                </span>
                <span role="columnheader" className="text-right">
                  {t('nutrition_summary.carbs')}
                </span>
                <span role="columnheader" className="text-right">
                  {t('nutrition_summary.fat')}
                </span>
                <span role="columnheader" className="text-right">
                  {t('nutrition_summary.fiber')}
                </span>
              </div>
              {pagedDays.map((day) => (
                <div
                  key={day.date}
                  role="row"
                  className="border-border text-12-5px grid grid-cols-[1.4fr_1fr_0.8fr_0.8fr_0.8fr_0.8fr] gap-2 border-t px-4 py-2.5"
                >
                  <span className="font-semibold">{day.date.slice(0, 10)}</span>
                  <span className="tnum text-right font-semibold">
                    {formatNumber(day.calories)}
                  </span>
                  <span className="text-text-2 tnum text-right">{day.protein.toFixed(1)}</span>
                  <span className="text-text-2 tnum text-right">{day.carbs.toFixed(1)}</span>
                  <span className="text-text-2 tnum text-right">{day.fat.toFixed(1)}</span>
                  <span className="text-text-2 tnum text-right">{day.fiber.toFixed(1)}</span>
                </div>
              ))}
            </div>
          </Card>
          <Pagination
            page={tablePage}
            pageSize={tablePageSize}
            totalCount={days.length}
            onPageChange={setTablePage}
            onPageSizeChange={(size) => {
              setTablePageSize(size);
              setTablePage(1);
            }}
          />
        </>
      )}
    </div>
  );
}

function SplitRow({
  label,
  split,
  dim = false,
}: {
  label: string;
  split: { protein: number; carbs: number; fat: number };
  dim?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1">
      <div className="text-muted-foreground text-10-5px font-semibold uppercase">{label}</div>
      <div className={cn('flex h-3.5 overflow-hidden rounded-full', dim && 'opacity-40')}>
        {(['protein', 'carbs', 'fat'] as const).map((m) => (
          <div key={m} style={{ width: `${String(split[m])}%`, background: `var(--color-${m})` }} />
        ))}
      </div>
    </div>
  );
}
