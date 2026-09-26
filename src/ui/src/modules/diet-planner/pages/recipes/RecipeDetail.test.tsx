import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Suspense } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import RecipeDetail from './RecipeDetail';

import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useCreateMeal: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
vi.mock('@modules/diet-planner/unitLabel', () => ({ unitLabel: (u: string) => u }));
vi.mock('react-router-dom', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports -- importOriginal generic needs the import() type
  const actual = await orig<typeof import('react-router-dom')>();
  return { ...actual, useParams: () => ({ id: 'r1' }), useNavigate: () => vi.fn() };
});
const recipe = {
  id: 'r1',
  name: 'Chicken & rice',
  servings: 2,
  prepTimeMinutes: 25,
  isOwner: true,
  visibility: 'Private',
  canEdit: true,
  description: '',
  instructions: 'Grill the chicken.\nCook the rice.\nCombine and serve.',
  ingredients: [
    { id: 'i1', productName: 'Chicken', amount: 300, unit: 'g' },
    { id: 'i2', productName: 'Rice', amount: 150, unit: 'g' },
  ],
  nutritionPerServing: { calories: 600, protein: 45, carbs: 60, fat: 18, fiber: 4 },
};

vi.mock('@modules/diet-planner/api/hooks/useRecipes', () => ({
  useDeleteRecipe: () => ({ mutateAsync: vi.fn(), isPending: false }),
  recipeOptions: (id: string) => ({
    queryKey: ['recipes', id],
    queryFn: () => Promise.resolve(recipe),
  }),
}));

function renderDetail() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <MemoryRouter>
        <Suspense fallback={<div role="status">loading</div>}>
          <RecipeDetail />
        </Suspense>
      </MemoryRouter>
    </Wrapper>,
  );
}

describe('RecipeDetail', () => {
  it('renders the ingredients at the recipe base servings', async () => {
    renderDetail();
    expect(await screen.findByText('300.0 g')).toBeInTheDocument();
    expect(screen.getByText('150.0 g')).toBeInTheDocument();
  });

  it('rescales ingredient amounts when servings change', async () => {
    renderDetail();
    await userEvent.click(
      await screen.findByRole('button', { name: 'recipe_detail.increase_servings' }),
    );
    // 2 -> 3 servings, ratio 1.5
    expect(screen.getByText('450.0 g')).toBeInTheDocument();
    expect(screen.getByText('225.0 g')).toBeInTheDocument();
  });

  it('splits the instructions into a numbered method', async () => {
    renderDetail();
    expect(await screen.findByText('Grill the chicken.')).toBeInTheDocument();
    expect(screen.getByText('Cook the rice.')).toBeInTheDocument();
  });

  it('shows the visibility badge and the edit controls for an editor', async () => {
    renderDetail();
    expect(await screen.findByText('visibility.Private')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'common.edit' })).toBeInTheDocument();
  });

  it('hides edit and delete when the caller cannot change the recipe', async () => {
    recipe.canEdit = false;
    renderDetail();
    expect(await screen.findByText('Grill the chicken.')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'common.edit' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'common.delete' })).not.toBeInTheDocument();
    recipe.canEdit = true;
  });
});
