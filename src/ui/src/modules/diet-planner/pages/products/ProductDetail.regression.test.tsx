import { render, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, it, vi } from 'vitest';

import ProductDetail from './ProductDetail';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

it('shows nutrition once, visualizes complete macros, and keeps fiber separate', async () => {
  server.use(
    http.get('http://localhost:5050/api/v1/products/:id', () =>
      HttpResponse.json({
        id: '11111111-1111-1111-1111-111111111111',
        name: 'Chicken Breast',
        calories: 165,
        protein: 31,
        carbs: 0,
        fat: 3.6,
        fiber: null,
        defaultUnit: 'g',
        isOwner: true,
      }),
    ),
  );
  const Wrapper = createWrapper();
  render(
    <Wrapper>
      <MemoryRouter initialEntries={['/products/11111111-1111-1111-1111-111111111111']}>
        <Routes>
          <Route path="/products/:id" element={<ProductDetail />} />
        </Routes>
      </MemoryRouter>
    </Wrapper>,
  );
  await screen.findByRole('heading', { name: 'product_detail.nutrition_facts' });
  expect(screen.getAllByText('products.table.protein')).toHaveLength(1);
  expect(screen.getAllByText('0.0 g')).toHaveLength(1);
  expect(screen.getByText('—')).toBeInTheDocument();
  expect(screen.getByText('product_detail.per_100g')).toBeInTheDocument();
  expect(
    screen.getByRole('img', { name: 'product_detail.macro_balance_description' }),
  ).toBeInTheDocument();
  expect(screen.getByText('(90%)')).toBeInTheDocument();
  expect(screen.getByText('(0%)')).toBeInTheDocument();
  expect(screen.getByText('(10%)')).toBeInTheDocument();
});

it('does not infer macro shares when a core macro is missing', async () => {
  server.use(
    http.get('http://localhost:5050/api/v1/products/:id', () =>
      HttpResponse.json({
        id: '22222222-2222-2222-2222-222222222222',
        name: 'Incomplete product',
        calories: 100,
        protein: 12,
        carbs: 8,
        fat: null,
        fiber: 2,
        defaultUnit: 'g',
        isOwner: true,
      }),
    ),
  );
  const Wrapper = createWrapper();
  render(
    <Wrapper>
      <MemoryRouter initialEntries={['/products/22222222-2222-2222-2222-222222222222']}>
        <Routes>
          <Route path="/products/:id" element={<ProductDetail />} />
        </Routes>
      </MemoryRouter>
    </Wrapper>,
  );

  await screen.findByRole('heading', { name: 'product_detail.nutrition_facts' });
  expect(screen.queryByRole('img')).not.toBeInTheDocument();
  expect(screen.queryByText(/%/)).not.toBeInTheDocument();
});
