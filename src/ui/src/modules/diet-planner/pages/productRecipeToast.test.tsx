/**
 * Tests that product and recipe create/edit pages show success/error toasts
 * on form submission — replacing bare console.error calls.
 *
 * Form components are stubbed so tests focus on the toast behaviour only,
 * not on form validation or rendering details.
 */

import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, it, expect, vi } from 'vitest';

import { createWrapper } from '../../../test/utils/queryWrapper';

import ProductCreate from './products/ProductCreate';
import ProductEdit from './products/ProductEdit';
import RecipeCreate from './recipes/RecipeCreate';
import RecipeEdit from './recipes/RecipeEdit';

import type { ReactNode } from 'react';

import { server } from '@/test/mocks/server';

const BASE = 'http://localhost:5000';

// ── i18n stub ──────────────────────────────────────────────────────────────────
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
  Trans: ({ i18nKey }: { i18nKey: string }) => i18nKey,
}));

// ── Routing stub ───────────────────────────────────────────────────────────────
vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
  useParams: () => ({ id: '11111111-1111-1111-1111-111111111111' }),
}));

// ── Token interceptor stub ────────────────────────────────────────────────────
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

// ── Form stubs — bypass validation; tests focus on toast feedback only ─────────
vi.mock('../components/products/ProductForm', () => ({
  ProductForm: ({
    onSubmit,
    submitLabel,
  }: {
    onSubmit: (data: {
      name: string;
      caloriesPer100g: number;
      proteinPer100g: number;
      carbsPer100g: number;
      fatPer100g: number;
      defaultUnit: string;
    }) => Promise<void>;
    submitLabel: string;
    isSubmitting: boolean;
    defaultValues?: object;
  }) => (
    <button
      onClick={() =>
        void onSubmit({
          name: 'Test Product',
          caloriesPer100g: 100,
          proteinPer100g: 10,
          carbsPer100g: 10,
          fatPer100g: 5,
          defaultUnit: 'g',
        })
      }
    >
      {submitLabel}
    </button>
  ),
}));

vi.mock('../components/recipes/RecipeForm', () => ({
  RecipeForm: ({
    onSubmit,
    submitLabel,
  }: {
    onSubmit: (data: {
      name: string;
      servings: number;
      ingredients: { productId: string; productName: string; amount: number; unit: string }[];
      description?: string;
      instructions?: string;
      prepTimeMinutes?: number;
    }) => Promise<void>;
    submitLabel: string;
    isSubmitting: boolean;
    defaultValues?: object;
  }) => (
    <button
      onClick={() =>
        void onSubmit({
          name: 'Test Recipe',
          servings: 2,
          ingredients: [
            {
              productId: '11111111-1111-1111-1111-111111111111',
              productName: 'Chicken Breast',
              amount: 200,
              unit: 'g',
            },
          ],
        })
      }
    >
      {submitLabel}
    </button>
  ),
}));

// ── Render helper ──────────────────────────────────────────────────────────────
function renderPage(ui: ReactNode) {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>{ui}</ToastProvider>
    </Wrapper>,
  );
}

// ── ProductCreate ──────────────────────────────────────────────────────────────
describe('ProductCreate — toast feedback', () => {
  it('shows success toast after creating a product', async () => {
    renderPage(<ProductCreate />);

    await userEvent.click(screen.getByRole('button', { name: /product_form.create_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('product_form.create_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when product creation fails', async () => {
    server.use(http.post(`${BASE}/api/v1/products`, () => HttpResponse.json({}, { status: 500 })));

    renderPage(<ProductCreate />);

    await userEvent.click(screen.getByRole('button', { name: /product_form.create_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('product_form.create_error')).toBeInTheDocument();
    });
  });
});

// ── ProductEdit ────────────────────────────────────────────────────────────────
describe('ProductEdit — toast feedback', () => {
  it('shows success toast after updating a product', async () => {
    renderPage(<ProductEdit />);

    const button = await screen.findByRole('button', { name: /product_form.update_btn/i });
    await userEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('product_form.update_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when product update fails', async () => {
    server.use(
      http.put(`${BASE}/api/v1/products/:id`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage(<ProductEdit />);

    const button = await screen.findByRole('button', { name: /product_form.update_btn/i });
    await userEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('product_form.update_error')).toBeInTheDocument();
    });
  });
});

// ── RecipeCreate ───────────────────────────────────────────────────────────────
describe('RecipeCreate — toast feedback', () => {
  it('shows success toast after creating a recipe', async () => {
    renderPage(<RecipeCreate />);

    await userEvent.click(screen.getByRole('button', { name: /recipe_form.create_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('recipe_form.create_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when recipe creation fails', async () => {
    server.use(http.post(`${BASE}/api/v1/recipes`, () => HttpResponse.json({}, { status: 500 })));

    renderPage(<RecipeCreate />);

    await userEvent.click(screen.getByRole('button', { name: /recipe_form.create_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('recipe_form.create_error')).toBeInTheDocument();
    });
  });
});

// ── RecipeEdit ─────────────────────────────────────────────────────────────────
describe('RecipeEdit — toast feedback', () => {
  it('shows success toast after updating a recipe', async () => {
    renderPage(<RecipeEdit />);

    const button = await screen.findByRole('button', { name: /recipe_form.update_btn/i });
    await userEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('recipe_form.update_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when recipe update fails', async () => {
    server.use(
      http.put(`${BASE}/api/v1/recipes/:id`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage(<RecipeEdit />);

    const button = await screen.findByRole('button', { name: /recipe_form.update_btn/i });
    await userEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('recipe_form.update_error')).toBeInTheDocument();
    });
  });
});
