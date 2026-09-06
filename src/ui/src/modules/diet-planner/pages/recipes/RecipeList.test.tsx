import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import RecipeList from './RecipeList';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
vi.mock('@modules/diet-planner/api/hooks/useRecipes', () => ({
  useDeleteRecipe: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useRecipes: () => ({
    isLoading: false,
    error: null,
    data: {
      totalCount: 2,
      items: [
        {
          id: 'a',
          name: 'Protein bowl',
          servings: 2,
          prepTimeMinutes: 40,
          isOwner: true,
          nutritionPerServing: { calories: 500, protein: 45, carbs: 30, fat: 12 },
        },
        {
          id: 'b',
          name: 'Quick salad',
          servings: 1,
          prepTimeMinutes: 10,
          isOwner: true,
          nutritionPerServing: { calories: 200, protein: 8, carbs: 15, fat: 9 },
        },
      ],
    },
  }),
}));

function renderList() {
  return render(
    <MemoryRouter>
      <RecipeList />
    </MemoryRouter>,
  );
}

describe('RecipeList', () => {
  it('renders every recipe and a create tile', () => {
    renderList();
    expect(screen.getByText('Protein bowl')).toBeInTheDocument();
    expect(screen.getByText('Quick salad')).toBeInTheDocument();
    expect(screen.getByText('recipes.create_tile')).toBeInTheDocument();
  });

  it('the High protein filter keeps only high-protein recipes', async () => {
    renderList();
    await userEvent.click(screen.getByRole('radio', { name: 'recipes.filter_high_protein' }));
    expect(screen.getByText('Protein bowl')).toBeInTheDocument();
    expect(screen.queryByText('Quick salad')).not.toBeInTheDocument();
  });

  it('the Quick filter keeps only fast recipes', async () => {
    renderList();
    await userEvent.click(screen.getByRole('radio', { name: 'recipes.filter_quick' }));
    expect(screen.getByText('Quick salad')).toBeInTheDocument();
    expect(screen.queryByText('Protein bowl')).not.toBeInTheDocument();
  });
});
