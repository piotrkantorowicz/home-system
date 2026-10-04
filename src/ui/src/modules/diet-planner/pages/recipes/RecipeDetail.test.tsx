import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Suspense } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { HouseholdWrapper } from '../../../../test/utils/householdWrapper';

import RecipeDetail from './RecipeDetail';

import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
}));
let weekMeals: Record<string, unknown>[] = [];
vi.mock('@modules/diet-planner/api/hooks/useMeals', () => ({
  useCreateMeal: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useMeals: () => ({ data: weekMeals }),
}));
let goal: { dailyCalorieTarget: number | null } | null = { dailyCalorieTarget: 2000 };
vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  goalsOptions: () => ({ queryKey: ['goals'], queryFn: () => Promise.resolve(goal) }),
}));
// Only the product with id "prod-chicken" is visible to the caller.
vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  productOptions: (id: string) => ({
    queryKey: ['product', id],
    queryFn: () => Promise.resolve(id === 'prod-chicken' ? { id } : null),
  }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
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
    { id: 'i1', productId: 'prod-chicken', productName: 'Chicken', amount: 300, unit: 'g' },
    { id: 'i2', productId: 'prod-rice', productName: 'Rice', amount: 150, unit: 'g' },
    { id: 'i3', productId: 'prod-egg', productName: 'Egg', amount: 1, unit: 'piece' },
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
      <HouseholdWrapper>
        <MemoryRouter>
          <Suspense fallback={<div role="status">loading</div>}>
            <RecipeDetail />
          </Suspense>
        </MemoryRouter>
      </HouseholdWrapper>
    </Wrapper>,
  );
}

describe('RecipeDetail', () => {
  it('renders the ingredients at the recipe base servings', async () => {
    renderDetail();
    expect(await screen.findByText('300 g')).toBeInTheDocument();
    expect(screen.getByText('150 g')).toBeInTheDocument();
    expect(screen.getByText('1 pc')).toBeInTheDocument();
  });

  it('rescales amounts and keeps fractional pieces when servings change', async () => {
    renderDetail();
    await userEvent.click(
      await screen.findByRole('button', { name: 'recipe_detail.increase_servings' }),
    );
    // 2 -> 3 servings, ratio 1.5
    expect(screen.getByText('450 g')).toBeInTheDocument();
    expect(screen.getByText('225 g')).toBeInTheDocument();
    expect(screen.getByText('1.5 pcs')).toBeInTheDocument();
  });

  it('links only the products the caller can open', async () => {
    renderDetail();
    expect(await screen.findByRole('link', { name: 'Chicken' })).toHaveAttribute(
      'href',
      '/diet-planner/products/prod-chicken',
    );
    expect(screen.queryByRole('link', { name: 'Rice' })).not.toBeInTheDocument();
  });

  it('splits the instructions into a numbered method', async () => {
    renderDetail();
    expect(await screen.findByText('Grill the chicken.')).toBeInTheDocument();
    expect(screen.getByText('Cook the rice.')).toBeInTheDocument();
  });

  it('shows the share of the day, and omits it without a goal', async () => {
    const view = renderDetail();
    expect(await screen.findByText(/recipe_detail.of_your_day.*30/)).toBeInTheDocument();
    view.unmount();
    goal = null;
    renderDetail();
    await screen.findByText('Grill the chicken.');
    expect(screen.queryByText(/recipe_detail.of_your_day/)).not.toBeInTheDocument();
    goal = { dailyCalorieTarget: 2000 };
  });

  it("lists this week's plan for the recipe only, and hides the section when empty", async () => {
    weekMeals = [
      {
        id: 'm1',
        recipeId: 'r1',
        date: '2026-10-05',
        mealSlotName: 'Dinner',
        mealSlotSortOrder: 2,
      },
      {
        id: 'm2',
        recipeId: 'other',
        date: '2026-10-06',
        mealSlotName: 'Lunch',
        mealSlotSortOrder: 1,
      },
    ];
    const view = renderDetail();
    const ctx = await screen.findByTestId('recipe-plan-context');
    expect(ctx).toHaveTextContent('Dinner');
    expect(ctx).not.toHaveTextContent('Lunch');
    view.unmount();
    weekMeals = [];
    renderDetail();
    await screen.findByText('Grill the chicken.');
    expect(screen.queryByTestId('recipe-plan-context')).not.toBeInTheDocument();
  });

  it('names the author only when it is the caller, never an auth subject', async () => {
    renderDetail();
    await screen.findByText('Grill the chicken.');
    // No roster member for the caller in this fixture: no byline at all.
    expect(screen.queryByText(/recipe_detail.by/)).not.toBeInTheDocument();
  });

  it('shows the visibility and the edit controls for an editor', async () => {
    renderDetail();
    expect(await screen.findByText(/visibility.Private/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'common.edit' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'common.actions' })).toBeInTheDocument();
  });

  it('hides edit and delete when the caller cannot change the recipe', async () => {
    recipe.canEdit = false;
    renderDetail();
    expect(await screen.findByText('Grill the chicken.')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'common.edit' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'common.actions' })).not.toBeInTheDocument();
    recipe.canEdit = true;
  });
});
