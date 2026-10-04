import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, it, expect, vi } from 'vitest';

import { TodayMeals } from './TodayMeals';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

const mocks = vi.hoisted(() => ({
  toastSuccess: vi.fn(),
  resetMutate: vi.fn(),
  completeMutateAsync: vi.fn(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
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

function renderCard(meals: MealEntryDto[], target: number | null = null) {
  return render(
    <MemoryRouter>
      <TodayMeals meals={meals} target={target} loading={false} onAddMeal={vi.fn()} />
    </MemoryRouter>,
  );
}

describe('TodayMeals', () => {
  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('lists every slot and features the earliest meal that is not eaten', () => {
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
    expect(screen.getAllByRole('listitem')).toHaveLength(3);
    expect(screen.getByText('Breakfast oats')).toBeInTheDocument();
    // 'Chicken bowl' is the earliest pending meal → it is the emphasised row
    expect(screen.getByText('Chicken bowl').closest('li.bg-accent')).not.toBeNull();
    expect(screen.getByText('Salmon').closest('li.bg-accent')).toBeNull();
    expect(
      screen.getByText(/dashboard.meals_eaten_count.*"eaten":1,"total":3/),
    ).toBeInTheDocument();
  });

  it('exposes each meal as a toggle and never strikes eaten meals through', () => {
    renderCard([
      meal({ id: 'a', recipeName: 'Eaten meal', status: 'Done', mealSlotDefaultTime: '08:00:00' }),
      meal({ id: 'b', recipeName: 'Pending meal', mealSlotDefaultTime: '20:00:00' }),
    ]);
    const toggles = screen.getAllByRole('button', { pressed: true });
    expect(toggles).toHaveLength(1);
    expect(screen.getAllByRole('button', { pressed: false })).toHaveLength(1);
    expect(screen.getByText('Eaten meal').className).not.toContain('line-through');
  });

  it('marks a meal eaten from its check toggle', async () => {
    mocks.completeMutateAsync.mockResolvedValue(undefined);
    renderCard([meal({ id: 'a', recipeName: 'Soup', mealSlotDefaultTime: '13:00:00' })]);

    await userEvent.click(
      screen.getByRole('button', { name: /dashboard.meal_mark /, pressed: false }),
    );

    expect(mocks.completeMutateAsync).toHaveBeenCalledWith('a');
  });

  it('unmarks an eaten meal from its check toggle', async () => {
    renderCard([meal({ id: 'a', recipeName: 'Soup', status: 'Done' })]);

    await userEvent.click(screen.getByRole('button', { pressed: true }));

    expect(mocks.resetMutate).toHaveBeenCalledWith('a', expect.anything());
  });

  it('leaves a meal that differs from the plan to the meal plan', () => {
    renderCard([meal({ id: 'a', recipeName: 'Soup', status: 'Modified' })]);

    expect(screen.getByRole('button', { pressed: true })).toBeDisabled();
  });

  it('summarises the planned total against the target', () => {
    const { unmount } = renderCard([meal({ id: 'a', calories: 1800 })], 2000);
    expect(screen.getByText(/dashboard.planned_footer_vs_target/)).toHaveTextContent(
      'dashboard.planned_under',
    );
    unmount();
    renderCard([meal({ id: 'a', calories: 2400 })], 2000);
    expect(screen.getByText(/dashboard.planned_footer_vs_target/)).toHaveTextContent(
      'dashboard.planned_over',
    );
  });

  it('renders an empty state with no meals', () => {
    renderCard([]);
    expect(screen.getByText('dashboard.no_meals')).toBeInTheDocument();
  });

  it('undoes a marked meal while the undo window is open', async () => {
    mocks.completeMutateAsync.mockResolvedValue(undefined);
    const { rerender } = renderCard([meal({ id: 'a', recipeName: 'Chicken bowl' })]);

    await userEvent.click(screen.getByRole('button', { name: 'dashboard.mark_eaten' }));
    rerender(
      <MemoryRouter>
        <TodayMeals
          target={null}
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
        <TodayMeals
          target={null}
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
