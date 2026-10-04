import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { DayMealList, type DayMealListProps } from './DayMealList';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { name?: string }) => (opts?.name ? `${key}|${opts.name}` : key),
    i18n: { language: 'en' },
  }),
}));

const slots = [{ id: 's1', name: 'Breakfast', sortOrder: 0, defaultTime: '08:00:00' }];

const meal = (over: Partial<MealEntryDto> = {}): MealEntryDto =>
  ({
    id: 'm1',
    mealSlotId: 's1',
    recipeId: 'r1',
    recipeName: 'Oatmeal',
    status: 'Planned',
    servings: 1,
    calories: 400,
    protein: 15,
    carbs: 60,
    fat: 8,
    fiber: 6,
    ...over,
  }) as MealEntryDto;

function setup(over: Partial<DayMealListProps> = {}, m = meal()) {
  const handlers = {
    onAddMeal: vi.fn(),
    onEditMeal: vi.fn(),
    onDeleteMeal: vi.fn(),
    onCompleteMeal: vi.fn(),
    onResetMeal: vi.fn(),
    onOverrideMeal: vi.fn(),
  };
  render(
    <MemoryRouter>
      <DayMealList slots={slots} meals={[m]} {...handlers} {...over} />
    </MemoryRouter>,
  );
  return handlers;
}

describe('DayMealList meal row', () => {
  it('has one completion toggle that completes a planned meal', async () => {
    const h = setup();
    await userEvent.click(screen.getByRole('button', { name: 'dashboard.meal_mark|Oatmeal' }));
    expect(h.onCompleteMeal).toHaveBeenCalledOnce();
  });

  it('un-completes an eaten meal with the same toggle', async () => {
    const h = setup({}, meal({ status: 'Done' }));
    const toggle = screen.getByRole('button', { name: 'dashboard.meal_unmark|Oatmeal' });
    expect(toggle).toHaveAttribute('aria-pressed', 'true');
    await userEvent.click(toggle);
    expect(h.onResetMeal).toHaveBeenCalledOnce();
  });

  it('keeps swap, edit and delete in the overflow menu, without hover, and omits reset when planned', async () => {
    const h = setup();
    const trigger = screen.getByRole('button', { name: 'calendar.meal_actions.menu_for|Oatmeal' });
    await userEvent.click(trigger);
    expect(screen.queryByRole('menuitem', { name: 'calendar.meal_actions.reset' })).toBeNull();
    await userEvent.click(screen.getByRole('menuitem', { name: 'calendar.meal_actions.override' }));
    expect(h.onOverrideMeal).toHaveBeenCalledWith('m1');

    await userEvent.click(trigger);
    await userEvent.click(screen.getByRole('menuitem', { name: 'common.edit' }));
    expect(h.onEditMeal).toHaveBeenCalledOnce();

    await userEvent.click(trigger);
    await userEvent.click(screen.getByRole('menuitem', { name: 'common.delete' }));
    expect(h.onDeleteMeal).toHaveBeenCalledOnce();
  });

  it('offers reset in the menu once the meal is no longer planned', async () => {
    const h = setup({}, meal({ status: 'Done' }));
    await userEvent.click(
      screen.getByRole('button', { name: 'calendar.meal_actions.menu_for|Oatmeal' }),
    );
    await userEvent.click(screen.getByRole('menuitem', { name: 'calendar.meal_actions.reset' }));
    expect(h.onResetMeal).toHaveBeenCalledOnce();
  });

  it('returns focus to the menu trigger after Escape', async () => {
    setup();
    const trigger = screen.getByRole('button', { name: 'calendar.meal_actions.menu_for|Oatmeal' });
    await userEvent.click(trigger);
    await userEvent.keyboard('{Escape}');
    expect(trigger).toHaveFocus();
  });

  it('hides logging and planning controls for read-only viewers', () => {
    setup({ canLog: false, canPlan: false });
    expect(screen.queryByRole('button', { name: /meal_mark/ })).toBeNull();
    expect(screen.queryByRole('button', { name: /menu_for/ })).toBeNull();
  });
});
