import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import RecipeDetail from './RecipeDetail';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@modules/diet-planner/unitLabel', () => ({ unitLabel: (u: string) => u }));
vi.mock('react-router-dom', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports -- importOriginal generic needs the import() type
  const actual = await orig<typeof import('react-router-dom')>();
  return { ...actual, useParams: () => ({ id: 'r1' }), useNavigate: () => vi.fn() };
});
vi.mock('@modules/diet-planner/api/hooks/useRecipes', () => ({
  useDeleteRecipe: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useRecipe: () => ({
    isLoading: false,
    error: null,
    data: {
      id: 'r1',
      name: 'Chicken & rice',
      servings: 2,
      prepTimeMinutes: 25,
      isOwner: true,
      description: '',
      instructions: 'Grill the chicken.\nCook the rice.\nCombine and serve.',
      ingredients: [
        { id: 'i1', productName: 'Chicken', amount: 300, unit: 'g' },
        { id: 'i2', productName: 'Rice', amount: 150, unit: 'g' },
      ],
      nutritionPerServing: { calories: 600, protein: 45, carbs: 60, fat: 18, fiber: 4 },
    },
  }),
}));

function renderDetail() {
  return render(
    <MemoryRouter>
      <RecipeDetail />
    </MemoryRouter>,
  );
}

describe('RecipeDetail', () => {
  it('renders the ingredients at the recipe base servings', () => {
    renderDetail();
    expect(screen.getByText('300.0 g')).toBeInTheDocument();
    expect(screen.getByText('150.0 g')).toBeInTheDocument();
  });

  it('rescales ingredient amounts when servings change', async () => {
    renderDetail();
    await userEvent.click(screen.getByRole('button', { name: 'increase' }));
    // 2 -> 3 servings, ratio 1.5
    expect(screen.getByText('450.0 g')).toBeInTheDocument();
    expect(screen.getByText('225.0 g')).toBeInTheDocument();
  });

  it('splits the instructions into a numbered method', () => {
    renderDetail();
    expect(screen.getByText('Grill the chicken.')).toBeInTheDocument();
    expect(screen.getByText('Cook the rice.')).toBeInTheDocument();
  });
});
