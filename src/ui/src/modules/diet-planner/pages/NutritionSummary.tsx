import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import { useNutritionSummary } from '@modules/diet-planner/api/hooks/useMeals';
import { MacroKcalCard } from '@modules/diet-planner/components/MacroKcalCard';
import {
  buildNutritionRange,
  kcalSplit,
  rangeDates,
} from '@modules/diet-planner/utils/nutritionRange';
import { iso } from '@modules/diet-planner/utils/weekHistory';
import {
  Banner,
  Card,
  DailyBars,
  EmptyState,
  MetricTile,
  PageContainer,
  PageHeader,
  Pagination,
  SegmentedControl,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  type DailyBar,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { goalStatus } from '@shared/lib/format';
import { CalendarX } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

type Range = '7' | '30' | '90';

/** Daily totals show unpaginated up to a month; 90 days page through in month-sized chunks. */
const PAGE_SIZE = 31;

export default function NutritionSummary() {
  const { t } = useTranslation();
  const fmt = useFormat();
  const [range, setRange] = useState<Range>('7');
  const [page, setPage] = useState(1);

  const today = iso(new Date());
  const dates = rangeDates(today, Number(range));
  const from = dates[0] ?? today;

  const { data, isLoading, isError, refetch } = useNutritionSummary({ from, to: today });
  const { data: goals } = useGoals();

  const target = goals?.dailyCalorieTarget ?? null;
  const proteinGoal = goals?.proteinGrams ?? null;
  const summary = buildNutritionRange(data ?? [], dates, target);
  const compact = dates.length > 14;

  const proteinShort =
    proteinGoal !== null && summary.avgProtein !== null
      ? goalStatus(summary.avgProtein, proteinGoal, 'min')
      : null;
  const intake =
    target !== null && summary.avgCalories !== null
      ? goalStatus(summary.avgCalories, target, 'limit')
      : null;

  const bars: DailyBar[] = summary.days.map(({ date, row }, i) => {
    const d = new Date(`${date}T00:00:00`);
    const kcal = row ? Math.round(row.calories) : null;
    return {
      key: date,
      // Long ranges label only the first day of each week so 90 bars stay readable.
      label: !compact || i % 7 === 0 ? fmt.dayShort(d) : '',
      sublabel: '',
      value: kcal,
      text: kcal === null ? '' : fmt.energy(kcal),
      over: kcal !== null && goalStatus(kcal, target, 'limit').state === 'over',
      isToday: date === today,
      srText: `${fmt.dayShort(d)}: ${kcal === null ? t('dashboard.nothing_logged') : fmt.energy(kcal)}`,
    };
  });

  const split =
    summary.avgProtein !== null && summary.avgCarbs !== null && summary.avgFat !== null
      ? kcalSplit(summary.avgProtein, summary.avgCarbs, summary.avgFat)
      : null;
  const goalSplit = goals
    ? kcalSplit(goals.proteinGrams ?? 0, goals.carbsGrams ?? 0, goals.fatGrams ?? 0)
    : null;

  // Newest first; only days with a logged row.
  const rows = summary.days.filter((d) => d.row).reverse();
  const paged =
    rows.length > PAGE_SIZE ? rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE) : rows;
  const g = (n: number | null) => `${fmt.grams(n)} g`;
  const int = (n: number) => fmt.grams(Math.round(n));

  return (
    <PageContainer>
      <PageHeader
        title={t('nutrition_page.title')}
        subtitle={t('nutrition_page.subtitle')}
        actions={
          <SegmentedControl
            label={t('nutrition_page.range_label')}
            value={range}
            onChange={(r) => {
              setRange(r);
              setPage(1);
            }}
            options={[
              { value: '7', label: t('nutrition_page.range_7') },
              { value: '30', label: t('nutrition_page.range_30') },
              { value: '90', label: t('nutrition_page.range_90') },
            ]}
          />
        }
      />

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
      ) : summary.loggedCount === 0 ? (
        <EmptyState
          icon={CalendarX}
          title={t('nutrition_page.no_data')}
          description={t('nutrition_page.no_data_desc')}
        />
      ) : (
        <div className="flex flex-col gap-6">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <MetricTile
              label={t('nutrition_page.avg_intake')}
              value={fmt.energy(summary.avgCalories)}
              hint={
                intake === null
                  ? t('nutrition_page.no_goal')
                  : intake.state === 'onTarget'
                    ? t('nutrition_page.on_target', { target: fmt.energy(target) })
                    : t(`nutrition_page.intake_${intake.state === 'over' ? 'over' : 'under'}`, {
                        amount: fmt.energy(Math.abs(intake.diff)),
                        target: fmt.energy(target),
                      })
              }
            />
            <MetricTile
              label={t('nutrition_page.avg_protein')}
              value={g(summary.avgProtein)}
              hint={
                proteinShort === null
                  ? t('nutrition_page.no_goal')
                  : proteinShort.state === 'short'
                    ? t('nutrition_page.protein_short', {
                        amount: g(Math.abs(proteinShort.diff)),
                        goal: g(proteinGoal),
                      })
                    : t('nutrition_page.protein_met', { goal: g(proteinGoal) })
              }
            />
            <MetricTile
              label={t('nutrition_page.days_logged')}
              value={t('nutrition_page.logged_of', {
                logged: summary.loggedCount,
                total: dates.length,
              })}
              hint={t('nutrition_page.averages_note', { count: summary.loggedCount })}
            />
            <MetricTile
              label={t('nutrition_page.over_days')}
              value={target === null ? '—' : String(summary.overDays)}
              hint={
                target === null
                  ? t('nutrition_page.no_goal')
                  : summary.avgOver === null
                    ? t('nutrition_page.none_over')
                    : t('nutrition_page.over_each', { amount: fmt.energy(summary.avgOver) })
              }
            />
          </div>

          <Card className="flex flex-col gap-4 p-6">
            <div className="text-15px font-bold">{t('nutrition_page.intake_vs_target')}</div>
            <DailyBars
              days={bars}
              target={target}
              {...(target === null
                ? {}
                : {
                    targetLabel: t('dashboard.hero_target_amount', { amount: fmt.energy(target) }),
                  })}
              ariaLabel={t('nutrition_page.intake_vs_target')}
              overLabel={t('dashboard.week_over_marker')}
              missingText="—"
              compact={compact}
            />
          </Card>

          <MacroKcalCard actual={split} goal={goalSplit} />

          <Card className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('nutrition_page.date')}</TableHead>
                  <TableHead className="text-right">kcal</TableHead>
                  <TableHead>{t('nutrition_page.vs_target')}</TableHead>
                  <TableHead className="text-right">{t('nutrition_summary.protein')}</TableHead>
                  <TableHead className="text-right">{t('nutrition_summary.carbs')}</TableHead>
                  <TableHead className="text-right">{t('nutrition_summary.fat')}</TableHead>
                  <TableHead className="text-right">{t('nutrition_summary.fiber')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paged.map(({ date, row }) => {
                  if (!row) return null;
                  const status = goalStatus(row.calories, target, 'limit');
                  const amount = fmt.energy(Math.abs(status.diff));
                  return (
                    <TableRow key={date} data-date={date} className="h-11">
                      <TableCell className="py-0 font-semibold">
                        {fmt.dayShort(new Date(`${date}T00:00:00`))}
                      </TableCell>
                      <TableCell className="tnum py-0 text-right font-semibold">
                        {fmt.energy(row.calories)}
                      </TableCell>
                      <TableCell className="text-text-2 py-0">
                        {status.state === 'none'
                          ? '—'
                          : status.state === 'onTarget'
                            ? t('nutrition_page.status_on_target')
                            : t(`nutrition_page.status_${status.state}`, { amount })}
                      </TableCell>
                      <TableCell className="tnum text-text-2 py-0 text-right">
                        {int(row.protein)}
                      </TableCell>
                      <TableCell className="tnum text-text-2 py-0 text-right">
                        {int(row.carbs)}
                      </TableCell>
                      <TableCell className="tnum text-text-2 py-0 text-right">
                        {int(row.fat)}
                      </TableCell>
                      <TableCell className="tnum text-text-2 py-0 text-right">
                        {int(row.fiber)}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Card>
          {rows.length > PAGE_SIZE ? (
            <Pagination
              page={page}
              pageSize={PAGE_SIZE}
              totalCount={rows.length}
              onPageChange={setPage}
              onPageSizeChange={() => undefined}
              pageSizeOptions={[PAGE_SIZE]}
            />
          ) : null}
        </div>
      )}
    </PageContainer>
  );
}
