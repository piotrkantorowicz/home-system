import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { createWrapper } from '../../../../test/utils/queryWrapper';

import ImportWizard from './ImportWizard';

import { server } from '@/test/mocks/server';

const BASE = 'http://localhost:5050';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
}));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const plan = {
  productsToCreate: 7,
  productsToReuse: 14,
  recipesToCreate: 9,
  recipesToReuse: 3,
  mealEntriesToCreate: 35,
};

function validation(over: Record<string, unknown> = {}) {
  return HttpResponse.json({
    valid: true,
    canProceed: true,
    summary: { errors: 0, warnings: 0, info: 0 },
    issues: [],
    plan,
    ...over,
  });
}

function renderWizard() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>
        <MemoryRouter>
          <ImportWizard />
        </MemoryRouter>
      </ToastProvider>
    </Wrapper>,
  );
}

const planFile = (name = 'diet-plan.json') =>
  new File(
    [
      JSON.stringify({
        products: [],
        recipes: [],
        schedule: [
          { date: '2026-10-05', meals: [{ type: 'lunch', recipe: 'A', servings: 1 }] },
          { date: '2026-10-11', meals: [{ type: 'dinner', recipe: 'A', servings: 1 }] },
        ],
      }),
    ],
    name,
    { type: 'application/json' },
  );

describe('ImportWizard', () => {
  it('reviews a chosen file with the file name, counts and the dynamic meal count', async () => {
    server.use(http.post(`${BASE}/api/v1/meals/validate`, () => validation()));
    renderWizard();

    await userEvent.upload(screen.getByLabelText('import_wizard.upload.choose_file'), planFile());

    expect(await screen.findByText('diet-plan.json')).toBeInTheDocument();
    expect(screen.getByText('35')).toBeInTheDocument();
    expect(screen.getByText(/review.meals_detail.*"range":"5 Oct – 11 Oct"/)).toBeInTheDocument();
    expect(screen.getByText(/review.recipes_existing.*"count":3/)).toBeInTheDocument();
    expect(screen.getByText(/review.products_matched.*"count":14/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /review.import_meals.*"count":35/ })).toBeEnabled();
    expect(screen.getByText(/review.existing_kept_range/)).toBeInTheDocument();
  });

  it('goes straight to the review for the sample plan', async () => {
    server.use(http.post(`${BASE}/api/v1/meals/validate`, () => validation()));
    renderWizard();

    await userEvent.click(screen.getByRole('button', { name: 'import_wizard.upload.try_sample' }));

    expect(await screen.findByText('import_wizard.upload.sample_name')).toBeInTheDocument();
  });

  it('rejects a non-JSON file and recovers when a valid one is chosen', async () => {
    server.use(http.post(`${BASE}/api/v1/meals/validate`, () => validation()));
    renderWizard();
    const input = screen.getByLabelText('import_wizard.upload.choose_file');

    // The input's accept filter is the browser's job; the wizard must still refuse a wrong file itself.
    await userEvent.setup({ applyAccept: false }).upload(input, new File(['x'], 'plan.txt'));
    expect(await screen.findByText('import_wizard.upload.not_json')).toBeInTheDocument();

    await userEvent.upload(input, planFile());
    expect(await screen.findByText('diet-plan.json')).toBeInTheDocument();
    expect(screen.queryByText('import_wizard.upload.not_json')).not.toBeInTheDocument();
  });

  it('reports invalid pasted JSON and stays on the first step', async () => {
    renderWizard();

    fireEvent.change(screen.getByRole('textbox', { name: 'import_wizard.upload.paste_label' }), {
      target: { value: '{ nope' },
    });
    await userEvent.click(screen.getByRole('button', { name: 'import_wizard.upload.continue' }));

    expect(await screen.findByText('import_wizard.upload.invalid_json')).toBeInTheDocument();
    expect(screen.queryByText('import_wizard.review.back')).not.toBeInTheDocument();
  });

  it('shows warnings as rows and blocks the import on errors, with a way to re-check', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/validate`, () =>
        validation({
          canProceed: false,
          summary: { errors: 1, warnings: 1, info: 0 },
          issues: [
            {
              severity: 'warning',
              category: 'x',
              path: null,
              item: 'Feta',
              message: 'No fiber value.',
              resolution: null,
            },
            {
              severity: 'error',
              category: 'y',
              path: null,
              item: 'Brunch',
              message: 'Unknown meal type.',
              resolution: null,
            },
          ],
        }),
      ),
    );
    renderWizard();

    await userEvent.upload(screen.getByLabelText('import_wizard.upload.choose_file'), planFile());

    expect(await screen.findByText(/No fiber value\./)).toBeInTheDocument();
    expect(screen.getByText(/Unknown meal type\./)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /review.import_meals/ })).toBeDisabled();
    expect(
      screen.getByRole('button', { name: 'import_wizard.review.revalidate' }),
    ).toBeInTheDocument();
  });

  it('turns a validation server failure into a retryable error', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/validate`, () =>
        HttpResponse.json({ title: 'boom' }, { status: 500 }),
      ),
    );
    renderWizard();

    await userEvent.upload(screen.getByLabelText('import_wizard.upload.choose_file'), planFile());

    expect(await screen.findByText('import_wizard.review.cannot_proceed')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /review.import_meals/ })).toBeDisabled();
  });

  it('goes back to choose another file', async () => {
    server.use(http.post(`${BASE}/api/v1/meals/validate`, () => validation()));
    renderWizard();
    await userEvent.upload(screen.getByLabelText('import_wizard.upload.choose_file'), planFile());
    await screen.findByText('diet-plan.json');

    await userEvent.click(
      screen.getByRole('button', { name: 'import_wizard.review.choose_another' }),
    );

    expect(
      screen.getByRole('button', { name: 'import_wizard.upload.try_sample' }),
    ).toBeInTheDocument();
    expect(screen.queryByText('diet-plan.json')).not.toBeInTheDocument();
  });

  it('ends on an actionable success state', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/validate`, () => validation()),
      http.post(`${BASE}/api/v1/meals/import`, () => HttpResponse.json({ mealEntriesCreated: 35 })),
    );
    renderWizard();
    await userEvent.upload(screen.getByLabelText('import_wizard.upload.choose_file'), planFile());
    await userEvent.click(await screen.findByRole('button', { name: /review.import_meals/ }));

    await waitFor(() => {
      expect(screen.getByText('import_wizard.done.title')).toBeInTheDocument();
    });
    expect(screen.getByRole('link', { name: 'import_wizard.done.open_plan' })).toHaveAttribute(
      'href',
      '/diet-planner/calendar',
    );
    await userEvent.click(screen.getByRole('button', { name: 'import_wizard.done.another' }));
    expect(
      screen.getByRole('button', { name: 'import_wizard.upload.try_sample' }),
    ).toBeInTheDocument();
  });
});
