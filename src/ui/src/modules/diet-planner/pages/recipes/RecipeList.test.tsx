import { QueryClient } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import RecipeList from './RecipeList';

import { HouseholdWrapper } from '@/test/utils/householdWrapper';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    i18n: { language: 'en' },
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
const recipeFetch = vi.fn(() => Promise.resolve(null));
const useRecipesSpy = vi.fn();

vi.mock('@modules/diet-planner/api/hooks/useRecipes', () => ({
  useDeleteRecipe: () => ({ mutateAsync: vi.fn(), isPending: false }),
  recipeOptions: (id: string) => ({ queryKey: ['recipes', id], queryFn: recipeFetch }),
  useRecipes: (params: unknown) => {
    useRecipesSpy(params);
    return {
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
          {
            id: 'c',
            name: 'Plain water',
            servings: 5,
            prepTimeMinutes: null,
            isOwner: true,
            visibility: 'Public',
            canEdit: true,
            nutritionPerServing: { calories: 0, protein: 0, carbs: 0, fat: 0 },
          },
        ],
      },
    };
  },
}));

function renderList(myRole = 'Owner', route = '/') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const Wrapper = createWrapper(queryClient);
  const view = render(
    <Wrapper>
      <HouseholdWrapper myRole={myRole}>
        <MemoryRouter initialEntries={[route]}>
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

  it('the High protein filter is sent to the server and resets paging', async () => {
    renderList('Owner', '/?page=3');
    await userEvent.click(screen.getByRole('radio', { name: 'recipes.filter_high_protein' }));
    expect(useRecipesSpy.mock.calls.at(-1)?.[0]).toMatchObject({
      onlyHighProtein: true,
      onlyQuick: false,
      onlyMine: false,
      page: 1,
    });
  });

  it('the Under 15 min filter is sent to the server', async () => {
    renderList();
    await userEvent.click(screen.getByRole('radio', { name: 'recipes.filter_quick' }));
    expect(useRecipesSpy.mock.calls.at(-1)?.[0]).toMatchObject({ onlyQuick: true });
  });

  it('the Mine chip is part of the single filter group', async () => {
    renderList();
    await userEvent.click(screen.getByRole('radio', { name: 'recipes.filter_mine' }));
    expect(useRecipesSpy.mock.calls.at(-1)?.[0]).toMatchObject({ onlyMine: true });
  });

  it('describes the macro bar by energy share for screen readers', () => {
    renderList();
    const bar = within(screen.getByRole('listitem', { name: 'Protein bowl' })).getByRole('img');
    // 45 g protein = 180 kcal, 30 g carbs = 120, 12 g fat = 108, total 408
    expect(bar.getAttribute('aria-label')).toContain('"protein":44');
  });

  it('passes the plural count for 1, 2 and 5 servings', () => {
    renderList();
    expect(screen.getByText(/recipes\.servings \{"count":2\}/)).toBeInTheDocument();
    expect(screen.getByText(/recipes\.servings \{"count":1\}/)).toBeInTheDocument();
    expect(screen.getByText(/recipes\.servings \{"count":5\}/)).toBeInTheDocument();
  });

  it('omits the macro bar for zero nutrition and the time when prep is missing', () => {
    renderList();
    const card = screen.getByRole('listitem', { name: 'Plain water' });
    expect(within(card).queryByRole('img')).not.toBeInTheDocument();
    expect(within(card).queryByText(/prep_minutes/)).not.toBeInTheDocument();
    expect(within(card).getByText('0 kcal')).toBeInTheDocument();
  });

  it('shows Private and Public badges but not the Household default', () => {
    renderList();
    expect(screen.getByText('visibility.Private')).toBeInTheDocument();
    expect(screen.getByText('visibility.Public')).toBeInTheDocument();
    expect(screen.queryByText('visibility.Household')).not.toBeInTheDocument();
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
