/**
 * Tests that step 3 of ImportWizard shows:
 * - an inline error banner when the import API call fails
 * - an error toast when the import API call fails
 * - the Confirm Import button re-enables after failure
 */
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, it, expect, vi } from 'vitest';

import { server } from '@/test/mocks/server';
import { ToastProvider } from '@shared/context/ToastContext';
import { createWrapper } from '../../../../test/utils/queryWrapper';

import ImportWizard from './ImportWizard';

import type { ReactNode } from 'react';

const BASE = 'http://localhost:5000';

// ── i18n stub ─────────────────────────────────────────────────────────────────
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
  Trans: ({ i18nKey }: { i18nKey: string }) => i18nKey,
}));

// ── Routing stub ──────────────────────────────────────────────────────────────
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

// ── render helper ─────────────────────────────────────────────────────────────
function renderWizard() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>
        <ImportWizard />
      </ToastProvider>
    </Wrapper>,
  );
}

// ── helper: advance wizard to step 3 ─────────────────────────────────────────
const validJson = JSON.stringify({
  products: [
    {
      name: 'Chicken',
      caloriesPer100g: 165,
      proteinPer100g: 31,
      carbsPer100g: 0,
      fatPer100g: 3.6,
      fiberPer100g: 0,
      unit: 'g',
    },
  ],
  recipes: [],
  schedule: [],
});

async function advanceToStep3() {
  // Step 1: paste JSON and continue
  // Use fireEvent.change because userEvent.type misinterprets JSON's `{` and `}` as key modifiers
  const textarea = screen.getByRole('textbox');
  fireEvent.change(textarea, { target: { value: validJson } });
  await userEvent.click(screen.getByRole('button', { name: /import_wizard.step1.continue/i }));

  // Step 2: validate (MSW returns canProceed: true → auto-advances to step 3)
  await userEvent.click(screen.getByRole('button', { name: /import_wizard.step2.validate/i }));

  // Wait for step 3 heading to appear
  await screen.findByText('import_wizard.step3.title');
}

// ── tests ─────────────────────────────────────────────────────────────────────

describe('ImportWizard step 3 — import failure feedback', () => {
  it('shows inline error banner when import fails', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/import`, () =>
        HttpResponse.json({ title: 'Server error' }, { status: 500 }),
      ),
    );

    renderWizard();
    await advanceToStep3();

    await userEvent.click(screen.getByRole('button', { name: /import_wizard.step3.confirm/i }));

    // The inline error is a <p role="alert"> inside the step 3 card
    await waitFor(() => {
      expect(
        screen.getByText('import_wizard.import_error', { selector: 'p[role="alert"]' }),
      ).toBeInTheDocument();
    });
  });

  it('shows error toast when import fails', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/import`, () =>
        HttpResponse.json({ title: 'Server error' }, { status: 500 }),
      ),
    );

    renderWizard();
    await advanceToStep3();

    await userEvent.click(screen.getByRole('button', { name: /import_wizard.step3.confirm/i }));

    await waitFor(() => {
      // Both the inline banner and the toast render the same message text
      expect(screen.getAllByText('import_wizard.import_error').length).toBeGreaterThanOrEqual(2);
    });
  });

  it('re-enables Confirm Import button after import failure', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/import`, () =>
        HttpResponse.json({ title: 'Server error' }, { status: 500 }),
      ),
    );

    renderWizard();
    await advanceToStep3();

    const confirmButton = screen.getByRole('button', { name: /import_wizard.step3.confirm/i });

    await userEvent.click(confirmButton);

    await waitFor(() => {
      expect(
        screen.getByText('import_wizard.import_error', { selector: 'p[role="alert"]' }),
      ).toBeInTheDocument();
    });

    // Button must be re-enabled (not disabled) so user can retry
    expect(screen.getByRole('button', { name: /import_wizard.step3.confirm/i })).not.toBeDisabled();
  });

  it('does not advance to step 4 when import fails', async () => {
    server.use(
      http.post(`${BASE}/api/v1/meals/import`, () =>
        HttpResponse.json({ title: 'Server error' }, { status: 500 }),
      ),
    );

    renderWizard();
    await advanceToStep3();

    await userEvent.click(screen.getByRole('button', { name: /import_wizard.step3.confirm/i }));

    await waitFor(() => {
      expect(
        screen.getByText('import_wizard.import_error', { selector: 'p[role="alert"]' }),
      ).toBeInTheDocument();
    });

    // Step 4 success heading should NOT appear
    expect(screen.queryByText('import_wizard.step4.success_title')).not.toBeInTheDocument();
  });

  it('clears inline error when user retries and import succeeds', async () => {
    // First call fails, second succeeds
    let callCount = 0;
    server.use(
      http.post(`${BASE}/api/v1/meals/import`, () => {
        callCount += 1;
        if (callCount === 1) {
          return HttpResponse.json({ title: 'Server error' }, { status: 500 });
        }
        return HttpResponse.json({ importedCount: 2 }, { status: 200 });
      }),
    );

    renderWizard();
    await advanceToStep3();

    // First click — fails
    await userEvent.click(screen.getByRole('button', { name: /import_wizard.step3.confirm/i }));

    await waitFor(() => {
      expect(
        screen.getByText('import_wizard.import_error', { selector: 'p[role="alert"]' }),
      ).toBeInTheDocument();
    });

    // Second click — succeeds; error banner should disappear
    await userEvent.click(screen.getByRole('button', { name: /import_wizard.step3.confirm/i }));

    await waitFor(() => {
      expect(
        screen.queryByText('import_wizard.import_error', { selector: 'p[role="alert"]' }),
      ).not.toBeInTheDocument();
    });
  });
});
