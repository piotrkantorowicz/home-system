/**
 * Tests that the Hydration page shows success/error toasts for quick-add,
 * custom-add, and delete actions.
 */
import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, it, expect } from 'vitest';

import { createWrapper } from '../../../test/utils/queryWrapper';

import Hydration from './Hydration';

import type { ReactNode } from 'react';

import { server } from '@/test/mocks/server';

// ── i18n stub ─────────────────────────────────────────────────────────────────
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _params?: Record<string, unknown>) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
  Trans: ({ i18nKey }: { i18nKey: string }) => i18nKey,
}));

const BASE = 'http://localhost:5050';

// ── render helper ─────────────────────────────────────────────────────────────
function renderPage(ui: ReactNode) {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>{ui}</ToastProvider>
    </Wrapper>,
  );
}

// ── Quick-add ─────────────────────────────────────────────────────────────────
describe('Hydration page — quick-add toast feedback', () => {
  it('shows success toast after quick-add', async () => {
    renderPage(<Hydration />);

    // Wait for page to load (entries header is a reliable landmark)
    await screen.findByText('hydration.entries_header');

    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_glass/i }));

    await waitFor(() => {
      expect(screen.getByText('hydration.log_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when quick-add fails', async () => {
    server.use(
      http.post(`${BASE}/api/v1/hydration/intake`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage(<Hydration />);

    await screen.findByText('hydration.entries_header');

    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_glass/i }));

    await waitFor(() => {
      expect(screen.getByText('hydration.log_error')).toBeInTheDocument();
    });
  });
});

// ── Custom-add ────────────────────────────────────────────────────────────────
describe('Hydration page — custom-add toast feedback', () => {
  it('shows success toast after custom-add', async () => {
    renderPage(<Hydration />);

    await screen.findByText('hydration.entries_header');

    const amountInput = screen.getByLabelText('hydration.custom_amount_label');
    await userEvent.type(amountInput, '350');

    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('hydration.log_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when custom-add fails', async () => {
    server.use(
      http.post(`${BASE}/api/v1/hydration/intake`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage(<Hydration />);

    await screen.findByText('hydration.entries_header');

    const amountInput = screen.getByLabelText('hydration.custom_amount_label');
    await userEvent.type(amountInput, '350');

    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('hydration.log_error')).toBeInTheDocument();
    });
  });
});

// ── Delete ────────────────────────────────────────────────────────────────────
describe('Hydration page — delete toast feedback', () => {
  it('shows success toast after deleting an entry', async () => {
    renderPage(<Hydration />);

    // Wait for entries to load (MSW returns 2 entries)
    const deleteButtons = await screen.findAllByRole('button', {
      name: /hydration\.delete_entry_aria/i,
    });

    const firstDeleteButton = deleteButtons[0];
    if (!firstDeleteButton) throw new Error('No delete button found');
    await userEvent.click(firstDeleteButton);

    await waitFor(() => {
      expect(screen.getByText('hydration.delete_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when delete fails', async () => {
    server.use(
      http.delete(`${BASE}/api/v1/hydration/intake/:id`, () =>
        HttpResponse.json({}, { status: 500 }),
      ),
    );

    renderPage(<Hydration />);

    const deleteButtons = await screen.findAllByRole('button', {
      name: /hydration\.delete_entry_aria/i,
    });

    const firstDeleteButton = deleteButtons[0];
    if (!firstDeleteButton) throw new Error('No delete button found');
    await userEvent.click(firstDeleteButton);

    await waitFor(() => {
      expect(screen.getByText('hydration.delete_error')).toBeInTheDocument();
    });
  });
});
