import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { NextUpCard } from './NextUpCard';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useCompleteMeal: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

const meal = (over: Partial<MealEntryDto>): MealEntryDto =>
  ({
    id: 'm',
    mealSlotName: 'Lunch',
    mealSlotDefaultTime: '12:00:00',
    mealSlotSortOrder: 2,
    mealTime: null,
    recipeName: 'Something',
    status: 'Planned',
    calories: 500,
    protein: 30,
    carbs: 50,
    fat: 15,
    fiber: 5,
    ...over,
  }) as MealEntryDto;

function renderCard(meals: MealEntryDto[]) {
  return render(
    <MemoryRouter>
      <NextUpCard meals={meals} loading={false} onAddMeal={vi.fn()} />
    </MemoryRouter>,
  );
}

describe('NextUpCard', () => {
  it('features the earliest meal that is not eaten', () => {
    renderCard([
      meal({
        id: 'a',
        recipeName: 'Breakfast oats',
        mealSlotDefaultTime: '08:00:00',
        status: 'Done',
      }),
      meal({ id: 'b', recipeName: 'Chicken bowl', mealSlotDefaultTime: '13:00:00' }),
      meal({ id: 'c', recipeName: 'Salmon', mealSlotDefaultTime: '19:00:00' }),
    ]);
    // 'Chicken bowl' is the earliest pending meal → it is the featured block
    const featured = screen.getByText('Chicken bowl').closest('div.bg-accent');
    expect(featured).not.toBeNull();
    expect(screen.getByText('Salmon')).toBeInTheDocument();
  });

  it('renders an empty state with no meals', () => {
    renderCard([]);
    expect(screen.getByText('dashboard.no_meals')).toBeInTheDocument();
  });

  it('strikes through eaten meals', () => {
    renderCard([
      meal({ id: 'a', recipeName: 'Eaten meal', status: 'Done' }),
      meal({ id: 'b', recipeName: 'Pending meal', mealSlotDefaultTime: '20:00:00' }),
    ]);
    expect(screen.getByText('Eaten meal').className).toContain('line-through');
  });
});
