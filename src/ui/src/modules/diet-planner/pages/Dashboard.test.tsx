import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));

vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({ data: null }),
}));

vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useMeals: () => ({ data: [], isLoading: false }),
  useNutritionSummary: () => ({ data: [] }),
  useCreateMeal: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

vi.mock('@modules/diet-planner/components/dashboard/TodaySummary', () => ({
  TodaySummary: () => <div data-testid="today-summary" />,
}));
vi.mock('@modules/diet-planner/components/dashboard/TodayMeals', () => ({
  TodayMeals: () => <div data-testid="today-meals" />,
}));
vi.mock('@modules/diet-planner/components/dashboard/WeekReviewCard', () => ({
  WeekReviewCard: () => <div data-testid="week-review" />,
}));
vi.mock('@modules/diet-planner/components/diet-plans/MealForm', () => ({
  MealForm: () => null,
}));
vi.mock('@modules/diet-planner/components/settings', () => ({
  GoalsForm: () => null,
}));

describe('Dashboard', () => {
  it('renders the Today screen with the summary, meals and week review', async () => {
    const Dashboard = (await import('./Dashboard')).default;
    render(
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'dashboard.today_title' })).toBeInTheDocument();
    expect(screen.getByTestId('today-summary')).toBeInTheDocument();
    expect(screen.getByTestId('today-meals')).toBeInTheDocument();
    expect(screen.getByTestId('week-review')).toBeInTheDocument();
  });
});
