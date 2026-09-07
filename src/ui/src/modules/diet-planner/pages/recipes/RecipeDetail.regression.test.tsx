import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import RecipeDetail from './RecipeDetail';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, values?: { count?: number; calories?: string }) =>
      key === 'recipe_detail.total_calories'
        ? `Total: ${String(values?.calories)} kcal for ${String(values?.count)}`
        : key === 'common.unit.g'
          ? 'g'
          : key,
    i18n: { language: 'en' },
  }),
}));

function renderPage() {
  server.use(
    http.get('http://localhost:5050/api/v1/recipes/:id', () =>
      HttpResponse.json({
        id: 'recipe-1',
        name: 'Oat bowl',
        servings: 2,
        isOwner: false,
        ingredients: [{ id: 'ingredient-1', productName: 'Oats', amount: 100, unit: 'g' }],
        nutritionPerServing: { calories: 200, protein: 10, carbs: 30, fat: 4, fiber: 0 },
      }),
    ),
  );
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>
        <MemoryRouter initialEntries={['/recipes/recipe-1']}>
          <Routes>
            <Route path="/recipes/:id" element={<RecipeDetail />} />
            <Route path="/diet-planner/calendar" element={<p>Meal plan destination</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </Wrapper>,
  );
}

describe('Recipe detail', () => {
  it('offers schedule setup when there are no meal slots', async () => {
    server.use(
      http.get('http://localhost:5050/api/v1/meal-schedule', () => HttpResponse.json(null)),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'recipe_detail.add_to_plan' }));
    expect(
      await screen.findByRole('link', { name: 'meal_form.configure_schedule' }),
    ).toHaveAttribute('href', '/diet-planner/profile?section=meal-schedule');
    expect(screen.getByRole('button', { name: 'meal_form.add_btn' })).toBeDisabled();
  });

  it('scales ingredients and total calories while keeping one serving unchanged', async () => {
    renderPage();
    await screen.findByRole('heading', { name: 'Oat bowl' });
    expect(screen.getByText('100.0 g')).toBeInTheDocument();
    expect(screen.getByText('Total: 400 kcal for 2')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'recipe_detail.increase_servings' }));
    expect(screen.getByText('150.0 g')).toBeInTheDocument();
    expect(screen.getByText('Total: 600 kcal for 3')).toBeInTheDocument();
    expect(screen.getByText('200')).toBeInTheDocument();
    expect(screen.getByText('10.0 g')).toBeInTheDocument();
    expect(screen.getByText('0.0 g')).toBeInTheDocument();
  });

  it('prefills a shared recipe and keeps the form on failure, then navigates after retry', async () => {
    const submitted = vi.fn();
    let fail = true;
    server.use(
      http.post('http://localhost:5050/api/v1/meals', async ({ request }) => {
        submitted(await request.json());
        return fail
          ? HttpResponse.json({}, { status: 500 })
          : HttpResponse.json('meal-1', { status: 201 });
      }),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'recipe_detail.add_to_plan' }));
    expect(screen.getByLabelText('meal_form.recipe_label')).toHaveValue('Oat bowl');
    expect(screen.getByLabelText('meal_form.servings_label')).toHaveValue(2);
    await userEvent.selectOptions(
      screen.getByLabelText('meal_form.meal_type_label'),
      'cccccccc-cccc-cccc-cccc-cccccccccccc',
    );
    await userEvent.click(screen.getByRole('button', { name: 'meal_form.add_btn' }));
    await screen.findByText('meal_form.add_error');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(submitted).toHaveBeenCalledWith(
      expect.objectContaining({ recipeId: 'recipe-1', servings: 2 }),
    );
    fail = false;
    await userEvent.click(screen.getByRole('button', { name: 'meal_form.add_btn' }));
    await waitFor(() => expect(screen.getByText('Meal plan destination')).toBeInTheDocument());
  });
});
