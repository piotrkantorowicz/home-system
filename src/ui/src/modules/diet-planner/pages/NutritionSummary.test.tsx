import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, it, expect, vi } from 'vitest';

import NutritionSummary from './NutritionSummary';

const useNutritionSummary = vi.fn<(p: { from: string; to: string }) => unknown>();

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({
    data: { dailyCalorieTarget: 2000, proteinGrams: 140, carbsGrams: 240, fatGrams: 70 },
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useNutritionSummary: (p: { from: string; to: string }) => useNutritionSummary(p),
}));

function day(date: string, calories: number, protein = 120) {
  return { date, calories, protein, carbs: 200, fat: 70, fiber: 25 };
}
const ok = (data: unknown[]) => ({ data, isLoading: false, isError: false });

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date(2026, 9, 4, 12));
});
afterEach(() => {
  vi.useRealTimers();
});

describe('NutritionSummary', () => {
  it('shows an empty state with no data', () => {
    useNutritionSummary.mockReturnValue(ok([]));
    render(<NutritionSummary />);
    expect(screen.getByText('nutrition_page.no_data')).toBeInTheDocument();
  });

  it('shows an error banner instead of the empty state and retries', async () => {
    const refetch = vi.fn();
    useNutritionSummary.mockReturnValue({ data: [], isLoading: false, isError: true, refetch });
    render(<NutritionSummary />);

    expect(screen.getByRole('alert')).toHaveTextContent('dashboard.data_error');
    expect(screen.queryByText('nutrition_page.no_data')).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'dashboard.retry' }));
    expect(refetch).toHaveBeenCalledOnce();
  });

  it('states explicit denominators, goal statuses and logged totals', () => {
    useNutritionSummary.mockReturnValue(ok([day('2026-10-02', 1800), day('2026-10-03', 2400)]));
    render(<NutritionSummary />);

    expect(useNutritionSummary).toHaveBeenLastCalledWith({ from: '2026-09-28', to: '2026-10-04' });
    expect(screen.getByText('2 100 kcal')).toBeInTheDocument(); // average of 1800 and 2400
    expect(screen.getByText(/nutrition_page.logged_of.*"logged":2,"total":7/)).toBeInTheDocument();
    expect(screen.getByText(/nutrition_page.protein_short.*"amount":"20 g"/)).toBeInTheDocument();
    expect(screen.getByText(/nutrition_page.over_each.*"amount":"400 kcal"/)).toBeInTheDocument();

    const [, newest = document.body, older = document.body] = screen.getAllByRole('row');
    const rows = screen.getAllByRole('row');
    // header + 2 logged days, newest first
    expect(rows).toHaveLength(3);
    expect(within(newest).getByText('2 400 kcal')).toBeInTheDocument();
    expect(within(newest).getByText(/status_over.*"amount":"400 kcal"/)).toBeInTheDocument();
    expect(within(older).getByText(/status_under.*"amount":"200 kcal"/)).toBeInTheDocument();
  });

  it('renders kcal-based macro shares for eaten and goal', () => {
    useNutritionSummary.mockReturnValue(ok([day('2026-10-03', 2000)]));
    render(<NutritionSummary />);
    // 120 g P / 200 g C / 70 g F -> 480 / 800 / 630 kcal = 25 / 42 / 33 %
    expect(screen.getByText('25%')).toBeInTheDocument();
    expect(screen.getByText('42%')).toBeInTheDocument();
    expect(screen.getByText('33%')).toBeInTheDocument();
  });

  it('queries 30 and 90 days and pages beyond 31 rows', async () => {
    const dates = Array.from({ length: 40 }, (_, i) => `2026-09-${String(i + 1).padStart(2, '0')}`);
    useNutritionSummary.mockReturnValue(ok(dates.slice(0, 30).map((d) => day(d, 2000))));
    render(<NutritionSummary />);
    await userEvent.click(screen.getByRole('radio', { name: 'nutrition_page.range_30' }));
    expect(useNutritionSummary).toHaveBeenLastCalledWith({ from: '2026-09-05', to: '2026-10-04' });
    expect(screen.getAllByRole('row')).toHaveLength(27); // header + 26 logged days, no pagination

    const many = Array.from({ length: 45 }, (_, i) => {
      const d = new Date(2026, 9, 4 - i);
      return day(
        `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`,
        2000,
      );
    });
    useNutritionSummary.mockReturnValue(ok(many));
    await userEvent.click(screen.getByRole('radio', { name: 'nutrition_page.range_90' }));
    expect(useNutritionSummary).toHaveBeenLastCalledWith({ from: '2026-07-07', to: '2026-10-04' });
    expect(screen.getAllByRole('row')).toHaveLength(32); // header + first page of 31
  });
});
