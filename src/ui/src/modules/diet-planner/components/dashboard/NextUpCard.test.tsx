import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, it, expect, vi } from 'vitest';

import { NextUpCard } from './NextUpCard';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

const mocks = vi.hoisted(() => ({
  toastSuccess: vi.fn(),
  resetMutate: vi.fn(),
  completeMutateAsync: vi.fn(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: mocks.toastSuccess, error: vi.fn() }),
}));
vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useResetMeal: () => ({ mutate: mocks.resetMutate }),
  useCompleteMeal: () => ({ mutateAsync: mocks.completeMutateAsync, isPending: false }),
}));

interface ToastAction {
  action: { onClick: () => void };
}

function lastToastAction(): ToastAction['action'] {
  const options = mocks.toastSuccess.mock.calls.at(-1)?.[1] as ToastAction | undefined;
  if (!options) throw new Error('toast.success was not called');
  return options.action;
}

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
  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

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
    const featured = screen.getByText('Chicken bowl').closest('article.bg-accent');
    expect(featured).not.toBeNull();
    expect(screen.getByText('Salmon')).toBeInTheDocument();
  });

  it('renders an empty state with no meals', () => {
    renderCard([]);
    expect(screen.getByText('dashboard.no_meals')).toBeInTheDocument();
  });

  it('places eaten meals in a collapsed disclosure', () => {
    renderCard([
      meal({ id: 'a', recipeName: 'Eaten meal', status: 'Done' }),
      meal({ id: 'b', recipeName: 'Pending meal', mealSlotDefaultTime: '20:00:00' }),
    ]);
    expect(screen.getByText('Eaten meal').closest('details')).not.toHaveAttribute('open');
  });

  it('undoes a marked meal while the undo window is open', async () => {
    mocks.completeMutateAsync.mockResolvedValue(undefined);
    const { rerender } = renderCard([meal({ id: 'a', recipeName: 'Chicken bowl' })]);

    await userEvent.click(screen.getByRole('button', { name: 'dashboard.mark_eaten' }));
    rerender(
      <MemoryRouter>
        <NextUpCard
          meals={[meal({ id: 'a', recipeName: 'Chicken bowl', status: 'Done' })]}
          loading={false}
          onAddMeal={vi.fn()}
        />
      </MemoryRouter>,
    );

    lastToastAction().onClick();

    expect(mocks.resetMutate).toHaveBeenCalledWith('a', expect.anything());
  });

  it('ignores undo once the undo window has closed', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    mocks.completeMutateAsync.mockResolvedValue(undefined);
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    const { rerender } = renderCard([meal({ id: 'a', recipeName: 'Chicken bowl' })]);

    await user.click(screen.getByRole('button', { name: 'dashboard.mark_eaten' }));
    rerender(
      <MemoryRouter>
        <NextUpCard
          meals={[meal({ id: 'a', recipeName: 'Chicken bowl', status: 'Done' })]}
          loading={false}
          onAddMeal={vi.fn()}
        />
      </MemoryRouter>,
    );

    act(() => {
      vi.advanceTimersByTime(6001);
    });
    lastToastAction().onClick();

    expect(mocks.resetMutate).not.toHaveBeenCalled();
  });
});
