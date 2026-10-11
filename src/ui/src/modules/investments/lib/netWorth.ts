import { minorToDecimal, toMinor } from './money';

/** A wallet whose latest balance is older than this many days is flagged as stale. */
export const STALE_DAYS = 45;

/** Whole calendar days from `from` to `to` (both `YYYY-MM-DD`). */
export function daysBetween(from: string, to: string): number {
  return Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / 86_400_000);
}

/** Whether a balance dated `asOf` is stale on `today`. */
export function isStale(asOf: string, today: string): boolean {
  return daysBetween(asOf, today) > STALE_DAYS;
}

export interface HistoryPoint {
  date: string;
  total: string;
  incomplete: boolean;
}

export interface ChangeSinceJanuary {
  /** API decimal string, signed. */
  amount: string;
  /** Percent of the 31 December value; `null` when that was zero. */
  percent: number | null;
}

/**
 * The change from 31 December of the previous year to `total`, or `null` when that point is not in
 * the series or was incomplete (a partial baseline would mislead).
 */
export function changeSinceJanuary(
  points: readonly HistoryPoint[],
  asOf: string,
  total: string,
): ChangeSinceJanuary | null {
  const baselineDate = `${String(Number(asOf.slice(0, 4)) - 1)}-12-31`;
  const baseline = points.find((p) => p.date === baselineDate);
  if (!baseline || baseline.incomplete) return null;
  const base = toMinor(baseline.total);
  const delta = toMinor(total) - base;
  return {
    amount: minorToDecimal(delta),
    percent: base === 0 ? null : Math.round((delta / base) * 1000) / 10,
  };
}
