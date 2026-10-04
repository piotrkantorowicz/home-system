import { goalStatus } from '@shared/lib/format';

import { iso } from './weekHistory';

import type { DailyNutrition } from '../api/hooks/useMeals';

/** The `count` calendar days ending on `today` (`YYYY-MM-DD`), oldest first. */
export function rangeDates(today: string, count: number): string[] {
  const [y, m, d] = today.split('-').map(Number) as [number, number, number];
  return Array.from({ length: count }, (_, i) => iso(new Date(y, m - 1, d - (count - 1 - i))));
}

export interface RangeDay {
  date: string;
  /** `null` = nothing logged; a logged day with 0 kcal is still a row. */
  row: DailyNutrition | null;
}

export interface NutritionRange {
  days: RangeDay[];
  /** Days with a logged row; the denominator for every average below. */
  loggedCount: number;
  avgCalories: number | null;
  avgProtein: number | null;
  avgCarbs: number | null;
  avgFat: number | null;
  overDays: number;
  /** Mean kcal over target across the over days; `null` when there are none. */
  avgOver: number | null;
}

export function buildNutritionRange(
  nutrition: DailyNutrition[],
  dates: string[],
  target: number | null,
): NutritionRange {
  const byDate = new Map(nutrition.map((n) => [n.date.slice(0, 10), n]));
  const days = dates.map((date) => ({ date, row: byDate.get(date) ?? null }));
  const logged = days.flatMap((d) => (d.row ? [d.row] : []));
  const avg = (pick: (r: DailyNutrition) => number) =>
    logged.length === 0 ? null : logged.reduce((s, r) => s + pick(r), 0) / logged.length;

  const over = logged.filter((r) => goalStatus(r.calories, target, 'limit').state === 'over');
  return {
    days,
    loggedCount: logged.length,
    avgCalories: avg((r) => r.calories),
    avgProtein: avg((r) => r.protein),
    avgCarbs: avg((r) => r.carbs),
    avgFat: avg((r) => r.fat),
    overDays: over.length,
    avgOver:
      over.length === 0 || target === null
        ? null
        : over.reduce((s, r) => s + r.calories - target, 0) / over.length,
  };
}

export interface KcalSplit {
  protein: number;
  carbs: number;
  fat: number;
}

/** Share of calories (4/4/9 kcal per gram) in whole percent; `null` when there are no calories. */
export function kcalSplit(protein: number, carbs: number, fat: number): KcalSplit | null {
  const p = protein * 4;
  const c = carbs * 4;
  const f = fat * 9;
  const total = p + c + f;
  if (total <= 0) return null;
  const protein_ = Math.round((p / total) * 100);
  const carbs_ = Math.round((c / total) * 100);
  return { protein: protein_, carbs: carbs_, fat: Math.max(0, 100 - protein_ - carbs_) };
}
