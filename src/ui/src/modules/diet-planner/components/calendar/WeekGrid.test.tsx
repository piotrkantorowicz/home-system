import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import { WeekGrid } from './WeekGrid';

import type { DailyNutrition, MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

const MINUS = String.fromCharCode(0x2212);

function dateStr(offsetDays: number): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

const slots = [{ id: 's1', name: 'Breakfast', sortOrder: 0 }];

const meal = (over: Partial<MealEntryDto>): MealEntryDto =>
  ({
    id: 'm1',
    date: dateStr(0),
    mealSlotId: 's1',
    mealSlotName: 'Breakfast',
    recipeName: 'Oatmeal',
    status: 'Planned',
    calories: 400,
    protein: 15,
    carbs: 60,
    fat: 8,
    fiber: 6,
    ...over,
  }) as MealEntryDto;

function renderGrid(meals: MealEntryDto[], nutrition: DailyNutrition[], target: number | null) {
  const weekDays = Array.from({ length: 3 }, (_, i) => {
    const d = new Date();
    d.setDate(d.getDate() + i);
    return d;
  });
  return render(
    <WeekGrid
      weekDays={weekDays}
      slots={slots}
      meals={meals}
      nutritionByDate={new Map(nutrition.map((n) => [n.date, n]))}
      calorieTarget={target}
      loading={false}
      bulkPending={false}
      onAddMeal={vi.fn()}
      onEditMeal={vi.fn()}
      onCompleteMeal={vi.fn()}
      onResetMeal={vi.fn()}
      onOverrideMeal={vi.fn()}
      onDeleteMeal={vi.fn()}
      onBulkComplete={vi.fn()}
    />,
  );
}

const nutri = (date: string, calories: number): DailyNutrition => ({
  date,
  calories,
  protein: 0,
  carbs: 0,
  fat: 0,
  fiber: 0,
});

describe('WeekGrid', () => {
  it('renders a meal chip and dashed add buttons for empty cells', () => {
    renderGrid([meal({})], [], 2000);
    expect(screen.getByText('Oatmeal')).toBeInTheDocument();
    // 3 columns x 1 slot = 3 cells, one filled -> 2 add buttons
    expect(screen.getAllByTitle('meal_form.add_title')).toHaveLength(2);
  });

  it('shows a negative day-total delta in the success colour when under target', () => {
    renderGrid([meal({})], [nutri(dateStr(0), 1700)], 2000);
    const delta = screen.getByText(`${MINUS}300`);
    expect(delta.className).toContain('text-success');
  });

  it('shows a positive delta in the destructive colour when over target', () => {
    renderGrid([meal({})], [nutri(dateStr(0), 2250)], 2000);
    const delta = screen.getByText('+250');
    expect(delta.className).toContain('text-destructive');
  });
});
