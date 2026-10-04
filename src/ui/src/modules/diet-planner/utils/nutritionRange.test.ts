import { describe, expect, it } from 'vitest';

import { buildNutritionRange, kcalSplit, rangeDates } from './nutritionRange';

const row = (date: string, calories: number, protein = 100, carbs = 200, fat = 70) => ({
  date,
  calories,
  protein,
  carbs,
  fat,
  fiber: 20,
});

describe('nutritionRange', () => {
  it('builds 7/30/90 contiguous days ending today across a month boundary', () => {
    expect(rangeDates('2026-10-02', 7)).toEqual([
      '2026-09-26',
      '2026-09-27',
      '2026-09-28',
      '2026-09-29',
      '2026-09-30',
      '2026-10-01',
      '2026-10-02',
    ]);
    expect(rangeDates('2026-10-04', 30)).toHaveLength(30);
    expect(rangeDates('2026-10-04', 90)[0]).toBe('2026-07-07');
  });

  it('averages over logged days only and counts over-target days beyond the 3% band', () => {
    const dates = rangeDates('2026-10-04', 4);
    const r = buildNutritionRange(
      [row('2026-10-02T00:00:00', 1800), row('2026-10-03', 2300), row('2026-10-04', 2100)],
      dates,
      2000,
    );
    expect(r.loggedCount).toBe(3);
    expect(r.avgCalories).toBeCloseTo(2066.67, 1);
    expect(r.overDays).toBe(2); // 2300 and 2100 (> 3% over)
    expect(r.avgOver).toBe(200);
    expect(r.days[0]?.row).toBeNull();
  });

  it('has no averages or over days without data or a target', () => {
    const r = buildNutritionRange([], rangeDates('2026-10-04', 7), null);
    expect(r.avgCalories).toBeNull();
    expect(r.overDays).toBe(0);
  });

  it('splits calories by kcal, not grams, and sums to 100', () => {
    // 100 g protein = 400, 200 g carbs = 800, 70 g fat = 630 -> 1830 kcal
    expect(kcalSplit(100, 200, 70)).toEqual({ protein: 22, carbs: 44, fat: 34 });
    expect(kcalSplit(0, 0, 0)).toBeNull();
  });
});
