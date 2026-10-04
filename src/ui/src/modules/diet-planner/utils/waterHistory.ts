import { iso } from './weekHistory';

/** The `count` calendar days ending on `today` (`YYYY-MM-DD`), oldest first. */
export function lastDates(today: string, count: number): string[] {
  const [y, m, d] = today.split('-').map(Number) as [number, number, number];
  return Array.from({ length: count }, (_, i) => iso(new Date(y, m - 1, d - (count - 1 - i))));
}

export interface WaterDay {
  date: string;
  isToday: boolean;
  /** `null` = nothing logged (or the day failed to load). */
  totalMl: number | null;
}

export interface WaterHistory {
  days: WaterDay[];
  /** Completed days (today is still in progress and excluded). */
  completed: number;
  /** Completed days that reached the goal; a day with nothing logged counts as not met. */
  met: number;
  /** Completed days with nothing logged. */
  missing: number;
}

/** `totals[i]` belongs to `dates[i]`; the last date is today. */
export function buildWaterHistory(
  dates: string[],
  totals: (number | null)[],
  goalMl: number | null,
): WaterHistory {
  const days = dates.map((date, i) => {
    const total = totals[i] ?? null;
    return {
      date,
      isToday: i === dates.length - 1,
      totalMl: total !== null && total > 0 ? total : null,
    };
  });
  const done = days.filter((d) => !d.isToday);
  return {
    days,
    completed: done.length,
    met: goalMl === null ? 0 : done.filter((d) => d.totalMl !== null && d.totalMl >= goalMl).length,
    missing: done.filter((d) => d.totalMl === null).length,
  };
}
