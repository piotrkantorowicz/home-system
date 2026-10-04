import { goalStatus } from '@shared/lib/format';

import type { DailyNutrition } from '../api/hooks/useMeals';

export interface HistoryDay {
  date: string;
  isToday: boolean;
  /** `null` = nothing logged; `0` = logged with zero kcal. */
  calories: number | null;
  protein: number | null;
  over: boolean;
}

export interface WeekHistory {
  days: HistoryDay[];
  /** Completed days only: today is still in progress and is excluded. */
  fullDays: number;
  avgCalories: number | null;
  avgProtein: number | null;
  overDays: number;
}

export function iso(d: Date): string {
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

/** The 7 calendar days ending on `today` (`YYYY-MM-DD`), oldest first. */
export function buildWeekHistory(
  nutrition: DailyNutrition[],
  today: string,
  target: number | null,
): WeekHistory {
  const [y, m, d] = today.split('-').map(Number) as [number, number, number];
  const byDate = new Map(nutrition.map((n) => [n.date.slice(0, 10), n]));

  const days: HistoryDay[] = [];
  for (let i = 6; i >= 0; i--) {
    const date = iso(new Date(y, m - 1, d - i));
    const row = byDate.get(date);
    const calories = row ? Math.round(row.calories) : null;
    days.push({
      date,
      isToday: i === 0,
      calories,
      protein: row ? row.protein : null,
      over: calories !== null && goalStatus(calories, target, 'limit').state === 'over',
    });
  }

  const full = days.filter((day) => !day.isToday && day.calories !== null);
  const avg = (pick: (day: HistoryDay) => number | null) =>
    full.length === 0 ? null : full.reduce((s, day) => s + (pick(day) ?? 0), 0) / full.length;

  return {
    days,
    fullDays: full.length,
    avgCalories: avg((day) => day.calories),
    avgProtein: avg((day) => day.protein),
    overDays: full.filter((day) => day.over).length,
  };
}
