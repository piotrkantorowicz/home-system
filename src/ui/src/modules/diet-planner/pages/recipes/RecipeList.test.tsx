import { QueryClient } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import RecipeList from './RecipeList';

import { HouseholdWrapper } from '@/test/utils/householdWrapper';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
const recipeFetch = vi.fn(() => Promise.resolve(null));

vi.mock('@modules/diet-planner/api/hooks/useRecipes', () => ({
  useDeleteRecipe: () => ({ mutateAsync: vi.fn(), isPending: false }),
  recipeOptions: (id: string) => ({ queryKey: ['recipes', id], queryFn: recipeFetch }),
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
          visibility: 'Private',
          canEdit: true,
          nutritionPerServing: { calories: 500, protein: 45, carbs: 30, fat: 12 },
        },
        {
          id: 'b',
          name: 'Quick salad',
          servings: 1,
          prepTimeMinutes: 10,
          isOwner: false,
          visibility: 'Household',
          canEdit: false,
          nutritionPerServing: { calories: 200, protein: 8, carbs: 15, fat: 9 },
        },
      ],
    },
  }),
}));

function renderList(myRole = 'Owner') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const Wrapper = createWrapper(queryClient);
  const view = render(
    <Wrapper>
      <HouseholdWrapper myRole={myRole}>
        <MemoryRouter>
          <RecipeList />
        </MemoryRouter>
      </HouseholdWrapper>
    </Wrapper>,
  );
  return { ...view, queryClient };
}

describe('RecipeList', () => {
  beforeEach(() => {
    recipeFetch.mockClear();
  });

  it('prefetches the recipe detail when a card link is hovered', async () => {
    const { queryClient } = renderList();

    await userEvent.hover(screen.getByRole('link', { name: 'Protein bowl' }));

    expect(recipeFetch).toHaveBeenCalledOnce();
    expect(queryClient.getQueryState(['recipes', 'a'])).toBeDefined();
  });

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

  it('shows each recipe visibility as a badge', () => {
    renderList();
    expect(screen.getByText('visibility.Private')).toBeInTheDocument();
    expect(screen.getByText('visibility.Household')).toBeInTheDocument();
  });

  it('offers edit and delete only on recipes the caller can edit', async () => {
    renderList();
    const [editable, readOnly] = screen.getAllByRole('button', { name: 'common.actions' });
    if (!editable || !readOnly) throw new Error('expected an actions menu per item');

    await userEvent.click(readOnly);
    expect(screen.queryByRole('menuitem', { name: 'common.edit' })).not.toBeInTheDocument();
    await userEvent.keyboard('{Escape}');

    await userEvent.click(editable);
    expect(await screen.findByRole('menuitem', { name: 'common.edit' })).toBeInTheDocument();
  });

  it('hides every create entry point from a guest', () => {
    renderList('Guest');
    expect(screen.queryByText('recipes.create_recipe')).not.toBeInTheDocument();
    expect(screen.queryByText('recipes.create_tile')).not.toBeInTheDocument();
  });
});
