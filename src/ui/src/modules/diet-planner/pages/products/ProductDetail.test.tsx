import { render, screen } from '@testing-library/react';
import { Suspense } from 'react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import ProductDetail from './ProductDetail';

import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));
vi.mock('@modules/diet-planner/unitLabel', () => ({ unitLabel: (u: string) => u }));

const product = {
  id: 'p1',
  name: 'Rolled oats',
  caloriesPer100g: 1379,
  proteinPer100g: 13.5,
  carbsPer100g: 67.7,
  fatPer100g: 6.9,
  fiberPer100g: 10.1,
  defaultUnit: 'g',
  densityGramsPerMl: null,
  gramPerPiece: null,
  isOwner: true,
};

// The page reads the product through useSuspenseQuery(productOptions(id)); the mocked
// options resolve from this variable so each test can pick the loaded state.
const state: { data: unknown } = { data: product };

vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  productOptions: (id: string) => ({
    queryKey: ['products', id],
    queryFn: () => Promise.resolve(state.data),
  }),
  useDeleteProduct: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

function renderAt() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <MemoryRouter initialEntries={['/diet-planner/products/p1']}>
        <Suspense fallback={<div role="status">loading</div>}>
          <Routes>
            <Route path="/diet-planner/products/:id" element={<ProductDetail />} />
          </Routes>
        </Suspense>
      </MemoryRouter>
    </Wrapper>,
  );
}

describe('ProductDetail', () => {
  it('renders the product name and a breadcrumb back to the list', async () => {
    renderAt();

    expect(await screen.findByRole('heading', { name: 'Rolled oats' })).toBeInTheDocument();
    const crumb = screen.getByRole('link', { name: 'products.title' });
    expect(crumb).toHaveAttribute('href', '/diet-planner/products');
  });

  it('shows the macro rows with gram values and a thin-space calorie figure', async () => {
    renderAt();

    expect((await screen.findAllByText('13.5 g')).length).toBeGreaterThan(0);
    expect(screen.getAllByText('67.7 g').length).toBeGreaterThan(0);

    // formatNumber groups thousands with U+2009; RTL normalizes it to a plain space
    expect(screen.getByText(/1\s379/)).toBeInTheDocument();
  });

  it('shows a not-found message when the product is missing', async () => {
    state.data = null;
    renderAt();
    expect(await screen.findByText('product_detail.not_found')).toBeInTheDocument();
    state.data = product;
  });
});
