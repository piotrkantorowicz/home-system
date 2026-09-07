import { describe, expect, it } from 'vitest';

import { consumedNutrition } from './consumedNutrition';

import type { MealEntryDto } from '../api/hooks/useMeals';

const meal = (status: string, calories: number, date = '2026-09-03') =>
  ({ status, date, calories, protein: 10, carbs: 20, fat: 5, fiber: 3 }) as MealEntryDto;

describe('consumedNutrition', () => {
  it('excludes planned meals and sums effective Done/Modified DTO quantities by date', () => {
    expect(
      consumedNutrition([
        meal('Planned', 900),
        meal('Done', 400),
        meal('Modified', 250),
        meal('Done', 300, '2026-09-02'),
      ]),
    ).toEqual([
      { date: '2026-09-02', calories: 300, protein: 10, carbs: 20, fat: 5, fiber: 3 },
      { date: '2026-09-03', calories: 650, protein: 20, carbs: 40, fat: 10, fiber: 6 },
    ]);
  });
  it('removes consumed totals when a meal is reset to Planned', () => {
    expect(consumedNutrition([meal('Planned', 400)])).toEqual([]);
  });
  it('retains a logged day even when its calories are zero', () => {
    expect(consumedNutrition([meal('Done', 0)])).toHaveLength(1);
  });
});
