import { QueryClient } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import ProductList from './ProductList';

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
vi.mock('@modules/diet-planner/unitLabel', () => ({ unitLabel: (u: string) => u }));
const productFetch = vi.fn(() => Promise.resolve(null));
const useProductsSpy = vi.fn();

vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  useDeleteProduct: () => ({ mutateAsync: vi.fn(), isPending: false }),
  productOptions: (id: string) => ({ queryKey: ['products', id], queryFn: productFetch }),
  useProducts: (params: unknown) => {
    useProductsSpy(params);
    return {
      isLoading: false,
      error: null,
      data: {
        totalCount: 2,
        items: [
          {
            id: 'a',
            name: 'Chicken breast',
            caloriesPer100g: 165,
            proteinPer100g: 31,
            carbsPer100g: 0,
            fatPer100g: 3.6,
            fiberPer100g: 0,
            defaultUnit: 'g',
            isOwner: true,
            visibility: 'Public',
            canEdit: true,
          },
          {
            id: 'b',
            name: 'Mystery powder',
            caloriesPer100g: null,
            proteinPer100g: null,
            carbsPer100g: null,
            fatPer100g: null,
            defaultUnit: 'g',
            isOwner: false,
            visibility: 'Household',
            canEdit: false,
          },
          {
            id: 'c',
            name: 'Cooking oil',
            caloriesPer100g: 884,
            proteinPer100g: 0,
            carbsPer100g: 0,
            fatPer100g: 100,
            fiberPer100g: 0,
            defaultUnit: 'ml',
            densityGramsPerMl: null,
            isOwner: true,
            visibility: 'Private',
            canEdit: true,
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
          <ProductList />
        </MemoryRouter>
      </HouseholdWrapper>
    </Wrapper>,
  );
  return { ...view, queryClient };
}

describe('ProductList', () => {
  beforeEach(() => {
    productFetch.mockClear();
  });

  it('prefetches the product detail when a row link is hovered', async () => {
    const { queryClient } = renderList();

    await userEvent.hover(screen.getByRole('link', { name: 'Chicken breast' }));

    expect(productFetch).toHaveBeenCalledOnce();
    expect(queryClient.getQueryState(['products', 'a'])).toBeDefined();
  });

  it('flags a product with missing macros as incomplete', () => {
    renderList();
    expect(screen.getByText('products.incomplete_badge')).toBeInTheDocument();
    expect(screen.getByText('Chicken breast')).toBeInTheDocument();
  });

  it('requests server-side incomplete filtering and resets paging', async () => {
    renderList('Owner', '/?page=3');
    await userEvent.click(screen.getByRole('radio', { name: 'products.filter_incomplete' }));
    const last = useProductsSpy.mock.calls.at(-1)?.[0] as Record<string, unknown>;
    expect(last).toMatchObject({ onlyIncomplete: true, onlyMine: false, page: 1 });
  });

  it('requests the Mine filter from the server', async () => {
    renderList();
    await userEvent.click(screen.getByRole('radio', { name: 'products.only_mine' }));
    expect(useProductsSpy.mock.calls.at(-1)?.[0]).toMatchObject({ onlyMine: true });
  });

  it('sorts via the header: ascending, then descending, back to page 1', async () => {
    renderList('Owner', '/?page=2');
    await userEvent.click(screen.getByRole('button', { name: /products\.sort_by.*calories/ }));
    expect(useProductsSpy.mock.calls.at(-1)?.[0]).toMatchObject({
      sortBy: 'calories',
      sortDescending: false,
      page: 1,
    });
    await userEvent.click(screen.getByRole('button', { name: /products\.sort_by.*calories/ }));
    expect(useProductsSpy.mock.calls.at(-1)?.[0]).toMatchObject({
      sortBy: 'calories',
      sortDescending: true,
    });
    expect(screen.getByRole('columnheader', { name: /products\.table\.calories/ })).toHaveAttribute(
      'aria-sort',
      'descending',
    );
  });

  it('shows dashes for null nutrition, never zero', () => {
    renderList();
    const row = screen.getByRole('row', { name: /Mystery powder/ });
    expect(row.textContent).toContain('–');
  });

  it('flags ml products without density instead of relabelling per 100 ml', () => {
    renderList();
    expect(screen.getByText('products.no_density')).toBeInTheDocument();
  });

  it('shows Private and Public badges but not the Household default', () => {
    renderList();
    expect(screen.getByText('visibility.Public')).toBeInTheDocument();
    expect(screen.getByText('visibility.Private')).toBeInTheDocument();
    expect(screen.queryByText('visibility.Household')).not.toBeInTheDocument();
  });

  it('offers edit and delete only on products the caller can edit', async () => {
    renderList();
    const [editable, readOnly] = screen.getAllByRole('button', { name: 'common.actions' });
    if (!editable || !readOnly) throw new Error('expected an actions menu per item');

    await userEvent.click(readOnly);
    expect(screen.queryByRole('menuitem', { name: 'common.edit' })).not.toBeInTheDocument();
    await userEvent.keyboard('{Escape}');

    await userEvent.click(editable);
    expect(await screen.findByRole('menuitem', { name: 'common.edit' })).toBeInTheDocument();
  });

  it('hides the add-product button from a guest', () => {
    renderList('Guest');
    expect(screen.queryByText('products.add_product')).not.toBeInTheDocument();
  });
});
