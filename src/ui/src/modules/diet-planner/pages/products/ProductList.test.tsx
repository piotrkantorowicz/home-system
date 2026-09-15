import { QueryClient } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import ProductList from './ProductList';

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
vi.mock('@modules/diet-planner/unitLabel', () => ({ unitLabel: (u: string) => u }));
const productFetch = vi.fn(() => Promise.resolve(null));

vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  useDeleteProduct: () => ({ mutateAsync: vi.fn(), isPending: false }),
  productOptions: (id: string) => ({ queryKey: ['products', id], queryFn: productFetch }),
  useProducts: () => ({
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
        },
        {
          id: 'b',
          name: 'Mystery powder',
          caloriesPer100g: null,
          proteinPer100g: null,
          carbsPer100g: null,
          fatPer100g: null,
          defaultUnit: 'g',
          isOwner: true,
        },
      ],
    },
  }),
}));

function renderList() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const Wrapper = createWrapper(queryClient);
  const view = render(
    <Wrapper>
      <MemoryRouter>
        <ProductList />
      </MemoryRouter>
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

  it('the incomplete chip filters the list to incomplete rows', async () => {
    renderList();
    await userEvent.click(screen.getByRole('button', { name: /products\.incomplete_chip/ }));
    expect(screen.queryByText('Chicken breast')).not.toBeInTheDocument();
    expect(screen.getByText('Mystery powder')).toBeInTheDocument();
  });

  it('switches to the cards view', async () => {
    renderList();
    await userEvent.click(screen.getByRole('radio', { name: 'products.view_cards' }));
    // cards view shows the "kcal / 100 g" unit label
    expect(screen.getAllByText(/kcal \/ 100 g/).length).toBeGreaterThan(0);
  });
});
