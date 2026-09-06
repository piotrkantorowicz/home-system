import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import ProductDetail from './ProductDetail';

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

const state: { data: unknown; isLoading: boolean; error: unknown } = {
  data: product,
  isLoading: false,
  error: null,
};

vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  useProduct: () => state,
  useDeleteProduct: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

function renderAt() {
  return render(
    <MemoryRouter initialEntries={['/diet-planner/products/p1']}>
      <Routes>
        <Route path="/diet-planner/products/:id" element={<ProductDetail />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ProductDetail', () => {
  it('renders the product name and a breadcrumb back to the list', () => {
    renderAt();

    expect(screen.getByRole('heading', { name: 'Rolled oats' })).toBeInTheDocument();
    const crumb = screen.getByRole('link', { name: 'products.title' });
    expect(crumb).toHaveAttribute('href', '/diet-planner/products');
  });

  it('shows the macro rows with gram values and a thin-space calorie figure', () => {
    renderAt();

    expect(screen.getAllByText('13.5 g').length).toBeGreaterThan(0);
    expect(screen.getAllByText('67.7 g').length).toBeGreaterThan(0);

    // formatNumber groups thousands with U+2009; RTL normalizes it to a plain space
    expect(screen.getByText(/1\s379/)).toBeInTheDocument();
  });

  it('shows a not-found message when the product is missing', () => {
    state.data = null;
    state.error = new Error('nope');
    renderAt();
    expect(screen.getByText('product_detail.not_found')).toBeInTheDocument();
    state.data = product;
    state.error = null;
  });
});
