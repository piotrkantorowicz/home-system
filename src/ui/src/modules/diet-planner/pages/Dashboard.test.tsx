import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
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

vi.mock('@modules/diet-planner/components/dashboard/TodayHero', () => ({
  TodayHero: () => <div data-testid="today-hero" />,
}));
vi.mock('@modules/diet-planner/components/dashboard/NextUpCard', () => ({
  NextUpCard: () => <div data-testid="next-up" />,
}));
vi.mock('@modules/diet-planner/components/dashboard/WaterCard', () => ({
  WaterCard: () => <div data-testid="water-card" />,
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
  it('renders the Today screen with all four cards', async () => {
    const Dashboard = (await import('./Dashboard')).default;
    render(
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'dashboard.today_title' })).toBeInTheDocument();
    expect(screen.getByTestId('today-hero')).toBeInTheDocument();
    expect(screen.getByTestId('next-up')).toBeInTheDocument();
    expect(screen.getByTestId('water-card')).toBeInTheDocument();
    expect(screen.getByTestId('week-review')).toBeInTheDocument();
  });
});
