import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({ data: null }),
}));

vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useMeals: () => ({ data: [], isLoading: false }),
  useNutritionSummary: () => ({ data: [] }),
}));

vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  useProducts: () => ({ data: { totalCount: 0 }, isLoading: false }),
}));

vi.mock('@modules/diet-planner/api/hooks/useRecipes', () => ({
  useRecipes: () => ({ data: { totalCount: 0 }, isLoading: false }),
}));

vi.mock('@modules/diet-planner/api/hooks/useProfile', () => ({
  useProfile: () => ({ data: null }),
}));

vi.mock('@modules/diet-planner/components/WeightPredictionCard', () => ({
  WeightPredictionCard: () => null,
}));

describe('Dashboard goals CTA', () => {
  it('shows goals CTA when no goals are configured', async () => {
    const Dashboard = (await import('./Dashboard')).default;
    render(
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>,
    );
    expect(screen.getByText('dashboard.goals_cta_title')).toBeInTheDocument();
    // CTA is now a button that opens a sheet, not a navigation link
    expect(screen.getByText('dashboard.goals_cta_button')).toBeInTheDocument();
  });
});
