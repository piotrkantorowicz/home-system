import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import { WeekGrid } from './WeekGrid';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
}));

function dateStr(offsetDays: number): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

const slots = [{ id: 's1', name: 'Breakfast', sortOrder: 0, defaultTime: '08:00:00' }];

const meal = (over: Partial<MealEntryDto>): MealEntryDto =>
  ({
    id: 'm1',
    date: dateStr(0),
    mealSlotId: 's1',
    mealSlotName: 'Breakfast',
    mealSlotDefaultTime: '08:00:00',
    mealSlotSortOrder: 0,
    mealTime: null,
    recipeName: 'Oatmeal',
    status: 'Planned',
    calories: 400,
    protein: 15,
    carbs: 60,
    fat: 8,
    fiber: 6,
    ...over,
  }) as MealEntryDto;

function renderGrid(meals: MealEntryDto[], target: number | null, firstOffset = 0) {
  const weekDays = Array.from({ length: 3 }, (_, i) => {
    const d = new Date();
    d.setDate(d.getDate() + firstOffset + i);
    return d;
  });
  return render(
    <WeekGrid
      weekDays={weekDays}
      slots={slots}
      meals={meals}
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

describe('WeekGrid', () => {
  it('renders a meal chip and dashed add buttons for empty cells', () => {
    renderGrid([meal({})], 2000);
    expect(screen.getByText('Oatmeal')).toBeInTheDocument();
    // 3 columns x 1 slot = 3 cells, one filled -> 2 add buttons
    expect(screen.getAllByTitle('meal_form.add_title')).toHaveLength(2);
  });

  it('shows the slot with its default time', () => {
    renderGrid([], 2000);
    expect(screen.getByRole('rowheader')).toHaveTextContent('Breakfast');
    expect(screen.getByRole('rowheader')).toHaveTextContent('08:00');
  });

  it('marks an eaten meal solid with a check and never strikes it through', () => {
    renderGrid([meal({ status: 'Done' })], 2000);
    const chip = screen.getByRole('button', { name: /Oatmeal/ });
    expect(chip.className).toContain('border-border-strong');
    expect(chip.className).not.toContain('border-dashed');
    expect(chip.innerHTML).not.toContain('line-through');
    expect(screen.queryByText('calendar.week_grid.not_logged')).not.toBeInTheDocument();
  });

  it('flags a past meal that was never logged', () => {
    renderGrid([meal({ date: dateStr(-1) })], 2000, -1);
    expect(screen.getByText('calendar.week_grid.not_logged')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Oatmeal/ }).className).toContain('bg-warning-soft');
  });

  it('outlines the earliest planned meal today as next and dashes later ones', () => {
    renderGrid(
      [
        meal({ id: 'a', recipeName: 'Late dinner', mealSlotDefaultTime: '19:00:00' }),
        meal({ id: 'b', recipeName: 'Early lunch', mealSlotDefaultTime: '12:00:00' }),
        meal({ id: 'c', recipeName: 'Tomorrow', date: dateStr(1) }),
      ],
      2000,
    );
    expect(screen.getByRole('button', { name: /Early lunch/ })).toHaveTextContent(
      'calendar.week_grid.next',
    );
    expect(screen.getByRole('button', { name: /Late dinner/ })).not.toHaveTextContent(
      'calendar.week_grid.next',
    );
    expect(screen.getByRole('button', { name: /Late dinner/ }).className).toContain(
      'border-dashed',
    );
    expect(screen.getByRole('button', { name: /Tomorrow/ }).className).toContain('border-dashed');
  });

  it('totals planned and eaten meals and says how far the day is from the target', () => {
    const { unmount } = renderGrid([meal({ calories: 1700 })], 2000);
    expect(screen.getByText(/calendar.week_grid.total_under/)).toBeInTheDocument();
    unmount();
    renderGrid([meal({ calories: 2250, status: 'Done' })], 2000);
    const over = screen.getByText(/calendar.week_grid.total_over/);
    expect(over.className).toContain('text-over');
  });

  it('says on target inside the 3% band and shows nothing without a goal', () => {
    const { unmount } = renderGrid([meal({ calories: 2050 })], 2000);
    expect(screen.getByText(/calendar.week_grid.total_onTarget/)).toBeInTheDocument();
    unmount();
    renderGrid([meal({ calories: 2050 })], null);
    expect(screen.queryByText(/calendar.week_grid.total_/)).not.toBeInTheDocument();
  });
});
