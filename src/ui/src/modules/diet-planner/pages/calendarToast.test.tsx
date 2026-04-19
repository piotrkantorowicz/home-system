/**
 * Tests that the Calendar page shows success/error toasts for meal
 * create, update, and delete actions.
 *
 * MealForm is stubbed so tests focus on toast feedback only, bypassing
 * the recipe-search UI inside the real form.
 */
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, it, expect, vi } from 'vitest';

import { server } from '@/test/mocks/server';
import { ToastProvider } from '@shared/context/ToastContext';
import { createWrapper } from '../../../test/utils/queryWrapper';

import Calendar from './Calendar';

import type { ReactNode } from 'react';

const BASE = 'http://localhost:5000';

/** Return today's date as YYYY-MM-DD so the mock meal appears in the current week. */
function todayStr() {
  const d = new Date();
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${String(year)}-${month}-${day}`;
}

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
  useParams: () => ({}),
  Link: ({ children, to }: { children: ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

// ── Token interceptor stub ────────────────────────────────────────────────────
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

// ── MealForm stub — bypass the recipe-search UI; expose create/edit triggers ──
vi.mock('../components/diet-plans/MealForm', () => ({
  MealForm: ({
    open,
    onSubmit,
    mode,
  }: {
    open: boolean;
    onClose: () => void;
    onSubmit: (data: {
      date: string;
      mealType: string;
      recipeId: string;
      servings: number;
      notes: string;
    }) => void;
    initialDate?: string;
    initialMealType?: string;
    initialValues?: object;
    isSubmitting?: boolean;
    mode: 'create' | 'edit';
  }) => {
    if (!open) return null;
    const label = mode === 'create' ? 'meal_form.add_btn' : 'meal_form.save_btn';
    return (
      <button
        onClick={() => {
          onSubmit({
            date: '2024-01-15',
            mealType: 'breakfast',
            recipeId: '44444444-4444-4444-4444-444444444444',
            servings: 1,
            notes: '',
          });
        }}
      >
        {label}
      </button>
    );
  },
}));

// ── Render helper ──────────────────────────────────────────────────────────────
function renderPage() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>
        <Calendar />
      </ToastProvider>
    </Wrapper>,
  );
}

/** Override the meals GET to return a meal for today so it appears in the current week. */
function useTodayMeal() {
  beforeEach(() => {
    server.use(
      http.get(`${BASE}/api/v1/meals`, () =>
        HttpResponse.json([
          {
            id: '33333333-3333-3333-3333-333333333333',
            userId: 'user-1',
            date: todayStr(),
            mealType: 'breakfast',
            recipeId: '44444444-4444-4444-4444-444444444444',
            recipeName: 'Oatmeal',
            servings: 1,
            notes: null,
            mealTime: null,
            sequenceOrder: null,
            createdAt: `${todayStr()}T07:00:00Z`,
          },
        ]),
      ),
    );
  });
}

// ── Helpers to find meal action buttons ───────────────────────────────────────
async function getMealActionButtons() {
  const mealTitle = await screen.findByText('Oatmeal');
  // The meal card div has class "group ..." — walk up until we find it
  let mealItem = mealTitle.parentElement;
  while (mealItem && !mealItem.classList.contains('group')) {
    mealItem = mealItem.parentElement;
  }
  if (!mealItem) throw new Error('Could not find .group meal item');
  const buttons = mealItem.querySelectorAll('button');
  // First button = pencil (edit), last button = trash (delete)
  const editBtn = buttons[0] as HTMLElement;
  const deleteBtn = buttons[buttons.length - 1] as HTMLElement;
  return { editBtn, deleteBtn };
}

// ── Create meal ───────────────────────────────────────────────────────────────
describe('Calendar — create meal toast', () => {
  it('shows success toast after creating a meal', async () => {
    renderPage();

    // Wait for calendar to load and click the first "add meal" (+) button
    const addButtons = await screen.findAllByTitle('meal_form.add_title');
    const firstAddButton = addButtons[0];
    if (!firstAddButton) throw new Error('No add meal button found');
    await userEvent.click(firstAddButton);

    // MealForm stub is open — click submit
    await userEvent.click(screen.getByRole('button', { name: /meal_form\.add_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_form.add_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when creating a meal fails', async () => {
    server.use(http.post(`${BASE}/api/v1/meals`, () => HttpResponse.json({}, { status: 500 })));

    renderPage();

    const addButtons = await screen.findAllByTitle('meal_form.add_title');
    const firstAddButton = addButtons[0];
    if (!firstAddButton) throw new Error('No add meal button found');
    await userEvent.click(firstAddButton);

    await userEvent.click(screen.getByRole('button', { name: /meal_form\.add_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_form.add_error')).toBeInTheDocument();
    });
  });
});

// ── Edit meal ─────────────────────────────────────────────────────────────────
describe('Calendar — edit meal toast', () => {
  useTodayMeal();

  it('shows success toast after updating a meal', async () => {
    renderPage();

    const { editBtn } = await getMealActionButtons();
    await userEvent.click(editBtn);

    // MealForm stub is open in edit mode
    await userEvent.click(screen.getByRole('button', { name: /meal_form\.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_form.edit_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when updating a meal fails', async () => {
    server.use(http.put(`${BASE}/api/v1/meals/:id`, () => HttpResponse.json({}, { status: 500 })));

    renderPage();

    const { editBtn } = await getMealActionButtons();
    await userEvent.click(editBtn);

    await userEvent.click(screen.getByRole('button', { name: /meal_form\.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_form.edit_error')).toBeInTheDocument();
    });
  });
});

// ── Delete meal ───────────────────────────────────────────────────────────────
describe('Calendar — delete meal toast', () => {
  useTodayMeal();

  it('shows success toast after deleting a meal', async () => {
    renderPage();

    const { deleteBtn } = await getMealActionButtons();
    await userEvent.click(deleteBtn);

    // Confirm delete in the dialog
    await userEvent.click(screen.getByRole('button', { name: /common\.delete/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_form.delete_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when deleting a meal fails', async () => {
    server.use(
      http.delete(`${BASE}/api/v1/meals/:id`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage();

    const { deleteBtn } = await getMealActionButtons();
    await userEvent.click(deleteBtn);

    await userEvent.click(screen.getByRole('button', { name: /common\.delete/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_form.delete_error')).toBeInTheDocument();
    });
  });
});
