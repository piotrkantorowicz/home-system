import { overspendMinor, toMinorUnits } from '@shared/lib/format';

export interface LimitStatus {
  /** Exact overspend in minor units; `0n` at or under the limit, so a zero limit with any spend is over. */
  over: bigint;
  /** Minor units left under the limit (negative when over). */
  left: bigint;
  isOver: boolean;
  /** Share of the limit used, 0-100 (a zero limit reads 0 when untouched, 100 once spent). */
  percent: number;
  /** Where the limit sits on a bar sized to the larger of spent and limit, 0-100. */
  limitAt: number;
}

/** Spent against a limit, in exact minor units — no tolerance band, no floats on money. */
export function limitStatus(spent: string, limit: string): LimitStatus | null {
  const s = toMinorUnits(spent);
  const l = toMinorUnits(limit);
  if (s === null || l === null) return null;
  const over = overspendMinor(s, l);
  const scale = s > l ? s : l;
  const ratio = (n: bigint) => (scale === 0n ? 0 : Number((n * 10000n) / scale) / 100);
  return { over, left: l - s, isOver: over > 0n, percent: ratio(s), limitAt: ratio(l) };
}

/** Sum of two API decimals' minor units; unparseable input counts as 0. */
export function sumMinor(values: readonly string[]): bigint {
  return values.reduce((total, v) => total + (toMinorUnits(v) ?? 0n), 0n);
}
