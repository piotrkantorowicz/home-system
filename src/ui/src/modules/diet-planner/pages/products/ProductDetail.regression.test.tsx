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

it('shows nutrition once and distinguishes missing fiber from zero carbs', async () => {
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
  expect(screen.getByText('kcal / 100 g')).toBeInTheDocument();
});
