import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import { useNutritionSummary } from '@modules/diet-planner/api/hooks/useMeals';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  DatePicker,
  EmptyState,
  Label,
  Pagination,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@shared/components/ui';
import { BarChart2, CalendarX } from 'lucide-react';
import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';

import { MacroProgressBar } from '../components/MacroProgressBar';

function formatLocalDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${String(year)}-${month}-${day}`;
}

function getDefaultRange() {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const day = today.getDay();
  const diff = today.getDate() - day + (day === 0 ? -6 : 1);
  const weekStart = new Date(today);
  weekStart.setDate(diff);
  const weekEnd = new Date(weekStart);
  weekEnd.setDate(weekEnd.getDate() + 6);
  return { from: formatLocalDate(weekStart), to: formatLocalDate(weekEnd) };
}

export default function NutritionSummary() {
  const { t } = useTranslation();
  const defaultRange = getDefaultRange();

  const [draftFrom, setDraftFrom] = useState(defaultRange.from);
  const [draftTo, setDraftTo] = useState(defaultRange.to);
  const [appliedRange, setAppliedRange] = useState(defaultRange);
  const [tablePage, setTablePage] = useState(1);
  const [tablePageSize, setTablePageSize] = useState(25);

  const { data: nutritionSummary, isLoading } = useNutritionSummary({
    from: appliedRange.from,
    to: appliedRange.to,
  });
  const { data: goals } = useGoals();

  const days = nutritionSummary ?? [];

  const totals = useMemo(
    () =>
      (nutritionSummary ?? []).reduce(
        (acc, day) => ({
          calories: acc.calories + day.calories,
          protein: acc.protein + day.protein,
          carbs: acc.carbs + day.carbs,
          fat: acc.fat + day.fat,
          fiber: acc.fiber + day.fiber,
        }),
        { calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0 },
      ),
    [nutritionSummary],
  );

  const dayCount = days.length || 1;

  const averages = {
    calories: totals.calories / dayCount,
    protein: totals.protein / dayCount,
    carbs: totals.carbs / dayCount,
    fat: totals.fat / dayCount,
    fiber: totals.fiber / dayCount,
  };

  const pagedDays = useMemo(() => {
    const start = (tablePage - 1) * tablePageSize;
    return (nutritionSummary ?? []).slice(start, start + tablePageSize);
  }, [nutritionSummary, tablePage, tablePageSize]);

  const handleApply = () => {
    if (draftFrom && draftTo && draftFrom <= draftTo) {
      setAppliedRange({ from: draftFrom, to: draftTo });
      setTablePage(1);
    }
  };

  const rangeInDays =
    Math.round(
      (new Date(appliedRange.to).getTime() - new Date(appliedRange.from).getTime()) / 86400000,
    ) + 1;

  return (
    <div className="animate-fade-in-up mx-auto max-w-5xl p-8 lg:p-10">
      {/* Header */}
      <div className="mb-8">
        <div className="mb-3 flex items-center gap-3">
          <div className="rounded-xl bg-violet-500/10 p-2.5">
            <BarChart2 className="h-6 w-6 text-violet-600 dark:text-violet-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('nutrition_page.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('nutrition_page.subtitle')}</p>
      </div>

      {/* Date Range Picker */}
      <Card className="mb-6">
        <CardContent className="pt-6">
          <div className="flex flex-wrap items-end gap-4">
            <div>
              <Label>{t('nutrition_page.from')}</Label>
              <DatePicker
                testId="from-date-picker"
                value={draftFrom}
                onChange={(v) => {
                  setDraftFrom(v ?? '');
                }}
                className="w-44"
              />
            </div>
            <div>
              <Label>{t('nutrition_page.to')}</Label>
              <DatePicker
                testId="to-date-picker"
                value={draftTo}
                onChange={(v) => {
                  setDraftTo(v ?? '');
                }}
                className="w-44"
              />
            </div>
            <Button onClick={handleApply} disabled={!draftFrom || !draftTo || draftFrom > draftTo}>
              {t('nutrition_page.apply')}
            </Button>
          </div>
          {draftFrom && draftTo && draftFrom > draftTo && (
            <p role="alert" className="text-destructive mt-2 text-sm">
              {t('nutrition_page.date_range_error')}
            </p>
          )}
        </CardContent>
      </Card>

      {isLoading ? (
        <div className="text-muted-foreground flex items-center justify-center py-16">
          {t('common.loading')}
        </div>
      ) : days.length === 0 ? (
        <EmptyState
          icon={CalendarX}
          title={t('nutrition_page.no_data')}
          description={t('nutrition_page.no_data_desc')}
        />
      ) : (
        <>
          {/* Totals + Goal Progress */}
          <div className="mb-6 grid gap-6 md:grid-cols-2">
            {/* Totals */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base">
                  {t('nutrition_page.totals', { days: days.length })}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {[
                  {
                    label: t('nutrition_summary.calories'),
                    value: Math.round(totals.calories),
                    unit: 'kcal',
                  },
                  {
                    label: t('nutrition_summary.protein'),
                    value: totals.protein.toFixed(1),
                    unit: 'g',
                  },
                  {
                    label: t('nutrition_summary.carbs'),
                    value: totals.carbs.toFixed(1),
                    unit: 'g',
                  },
                  { label: t('nutrition_summary.fat'), value: totals.fat.toFixed(1), unit: 'g' },
                  {
                    label: t('nutrition_summary.fiber'),
                    value: totals.fiber.toFixed(1),
                    unit: 'g',
                  },
                ].map((item) => (
                  <div key={item.label} className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{item.label}</span>
                    <span className="font-medium tabular-nums">
                      {item.value} {item.unit}
                    </span>
                  </div>
                ))}
              </CardContent>
            </Card>

            {/* Daily Average */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base">{t('nutrition_page.daily_avg')}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {[
                  {
                    label: t('nutrition_summary.calories'),
                    value: Math.round(averages.calories),
                    unit: 'kcal',
                  },
                  {
                    label: t('nutrition_summary.protein'),
                    value: averages.protein.toFixed(1),
                    unit: 'g',
                  },
                  {
                    label: t('nutrition_summary.carbs'),
                    value: averages.carbs.toFixed(1),
                    unit: 'g',
                  },
                  { label: t('nutrition_summary.fat'), value: averages.fat.toFixed(1), unit: 'g' },
                  {
                    label: t('nutrition_summary.fiber'),
                    value: averages.fiber.toFixed(1),
                    unit: 'g',
                  },
                ].map((item) => (
                  <div key={item.label} className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{item.label}</span>
                    <span className="font-medium tabular-nums">
                      {item.value} {item.unit}
                    </span>
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>

          {/* Goal vs Actual (range total vs goal * days in range) */}
          {goals && (
            <Card className="mb-6">
              <CardHeader className="pb-3">
                <CardTitle className="text-base">
                  {t('nutrition_page.vs_goal', { days: rangeInDays })}
                </CardTitle>
              </CardHeader>
              <CardContent className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
                <MacroProgressBar
                  label={t('nutrition_summary.calories')}
                  actual={totals.calories}
                  goal={
                    goals.dailyCalorieTarget !== null
                      ? goals.dailyCalorieTarget * rangeInDays
                      : null
                  }
                  unit="kcal"
                  gradient="from-rose-500 to-orange-500"
                />
                <MacroProgressBar
                  label={t('nutrition_summary.protein')}
                  actual={totals.protein}
                  goal={goals.proteinGrams !== null ? goals.proteinGrams * rangeInDays : null}
                  gradient="from-blue-500 to-indigo-500"
                />
                <MacroProgressBar
                  label={t('nutrition_summary.carbs')}
                  actual={totals.carbs}
                  goal={goals.carbsGrams !== null ? goals.carbsGrams * rangeInDays : null}
                  gradient="from-emerald-500 to-teal-500"
                />
                <MacroProgressBar
                  label={t('nutrition_summary.fat')}
                  actual={totals.fat}
                  goal={goals.fatGrams !== null ? goals.fatGrams * rangeInDays : null}
                  gradient="from-amber-500 to-orange-500"
                />
                <MacroProgressBar
                  label={t('nutrition_summary.fiber')}
                  actual={totals.fiber}
                  goal={goals.fiberGrams !== null ? goals.fiberGrams * rangeInDays : null}
                  gradient="from-violet-500 to-purple-500"
                />
              </CardContent>
            </Card>
          )}

          {/* Daily Breakdown Table */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">{t('nutrition_page.daily_breakdown')}</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow className="bg-muted/30">
                    <TableHead>{t('nutrition_page.date')}</TableHead>
                    <TableHead className="text-right">
                      {t('nutrition_summary.calories')} (kcal)
                    </TableHead>
                    <TableHead className="text-right">
                      {t('nutrition_summary.protein')} (g)
                    </TableHead>
                    <TableHead className="text-right">{t('nutrition_summary.carbs')} (g)</TableHead>
                    <TableHead className="text-right">{t('nutrition_summary.fat')} (g)</TableHead>
                    <TableHead className="text-right">{t('nutrition_summary.fiber')} (g)</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pagedDays.map((day) => (
                    <TableRow key={day.date}>
                      <TableCell className="font-medium">{day.date}</TableCell>
                      <TableCell className="text-right tabular-nums">
                        {Math.round(day.calories)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {day.protein.toFixed(1)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {day.carbs.toFixed(1)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {day.fat.toFixed(1)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {day.fiber.toFixed(1)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
            <div className="px-6 pb-4">
              <Pagination
                page={tablePage}
                pageSize={tablePageSize}
                totalCount={days.length}
                onPageChange={setTablePage}
                onPageSizeChange={setTablePageSize}
              />
            </div>
          </Card>
        </>
      )}
    </div>
  );
}
