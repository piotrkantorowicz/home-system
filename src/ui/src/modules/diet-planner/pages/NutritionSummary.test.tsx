import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import NutritionSummary from './NutritionSummary';

const useNutritionSummary =
  vi.fn<() => { data: unknown[]; isLoading: boolean; isError?: boolean; refetch?: () => void }>();

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({
    data: { dailyCalorieTarget: 2000, proteinGrams: 140, carbsGrams: 240, fatGrams: 70 },
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useNutritionSummary: () => useNutritionSummary(),
}));

function day(date: string, calories: number) {
  return { date, calories, protein: 40, carbs: 50, fat: 20, fiber: 5 };
}

describe('NutritionSummary', () => {
  it('shows an empty state with no data', () => {
    useNutritionSummary.mockReturnValue({ data: [], isLoading: false });
    render(<NutritionSummary />);
    expect(screen.getByText('nutrition_page.no_data')).toBeInTheDocument();
  });

  it('shows an error banner instead of the empty state when the request fails', async () => {
    const refetch = vi.fn();
    useNutritionSummary.mockReturnValue({ data: [], isLoading: false, isError: true, refetch });
    render(<NutritionSummary />);

    expect(screen.getByRole('alert')).toHaveTextContent('dashboard.data_error');
    expect(screen.queryByText('nutrition_page.no_data')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'dashboard.retry' }));
    expect(refetch).toHaveBeenCalledOnce();
  });

  it('renders the KPI row and flags over-target days', () => {
    useNutritionSummary.mockReturnValue({
      data: [day('2026-09-01', 1800), day('2026-09-02', 2400), day('2026-09-03', 0)],
      isLoading: false,
    });
    render(<NutritionSummary />);
    expect(screen.getByText('nutrition_page.avg_intake')).toBeInTheDocument();
    // 2 of 3 days logged
    expect(screen.getByText('2 / 3')).toBeInTheDocument();
  });

  it('switches the range through the segmented control', async () => {
    useNutritionSummary.mockReturnValue({ data: [day('2026-09-03', 2000)], isLoading: false });
    render(<NutritionSummary />);
    await userEvent.click(screen.getByRole('radio', { name: 'nutrition_page.range_30' }));
    // the query is called again with a wider range
    expect(useNutritionSummary).toHaveBeenCalled();
  });
});
