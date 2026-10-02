import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetLayout } from '../components/BudgetLayout';
import { budgetModule } from '../index';
import { currentMonth, shiftMonth } from '../lib/dates';

import BudgetPage from './BudgetPage';

import type { FakeHouseholdState } from '@/test/utils/householdState';

import { server } from '@/test/mocks/server';
import { HouseholdStateContext } from '@/test/utils/householdState';
import { createWrapper } from '@/test/utils/queryWrapper';

const household: FakeHouseholdState = {
  household: { id: 'h1' },
  myRole: 'Owner',
  myPersonId: 'me',
  members: [
    { personId: 'me', displayName: 'Alex', role: 'Owner', isManaged: false },
    { personId: 'kid', displayName: 'Sam', role: 'Child', isManaged: true },
  ],
};
vi.mock('@modules/household', async () => {
  const { useFakeHousehold } = await import('@/test/utils/householdState');
  return { useHousehold: useFakeHousehold };
});
vi.mock('react-oidc-context', () => ({
  useAuth: () => ({ isAuthenticated: true, user: { profile: { sub: 'alex' } } }),
}));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: vi.fn(),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';
const envelope = (over: Record<string, unknown>) => ({
  accountId: 'a1',
  name: 'Everyday',
  visibility: 'Household',
  ownerPersonId: null,
  isArchived: false,
  spent: '0.00',
  limit: null,
  limitRevision: null,
  remaining: null,
  isOverspent: false,
  ...over,
});

let client: QueryClient;
let envelopes: Record<string, unknown>[];
let summaryStatus: number;
let summaryParams: URLSearchParams[];

function ui() {
  return (
    <HouseholdStateContext value={{ ...household }}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/budget']}>
          <Routes>
            <Route path="/budget" element={<BudgetLayout />}>
              <Route index element={<BudgetPage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </HouseholdStateContext>
  );
}

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <>{ui()}</>
    </Wrapper>,
  );
}

beforeEach(() => {
  initI18n([budgetModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.myRole = 'Owner';
  envelopes = [envelope({})];
  summaryStatus = 200;
  summaryParams = [];
  server.use(
    http.get(`${BASE}/api/budget`, () => HttpResponse.json({ id: 'b1', currency: 'PLN' })),
    http.get(`${BASE}/api/budget/summary`, ({ request }) => {
      summaryParams.push(new URL(request.url).searchParams);
      return summaryStatus === 200
        ? HttpResponse.json({
            month: '2026-10',
            scope: 'Shared',
            currency: 'PLN',
            totalSpent: '12.34',
            envelopes,
            categories: [{ category: 'Groceries', spent: '2.34' }],
          })
        : new HttpResponse(null, { status: summaryStatus });
    }),
    http.get(`${BASE}/api/budget/expenses`, () =>
      HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 5 }),
    ),
    http.get(`${BASE}/api/budget/accounts`, () =>
      HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 100 }),
    ),
  );
});

describe('Overview', () => {
  it('shows the server total, categories and a clearly separate shared scope', async () => {
    renderPage();
    await screen.findByText('12.34 PLN');
    expect(screen.getByText(/^Shared spending ·/)).toBeInTheDocument();
    expect(screen.getByText('Groceries')).toBeInTheDocument();
    expect(summaryParams[0]?.get('scope')).toBe('shared');
    expect(summaryParams[0]?.get('month')).toBe(currentMonth());
  });

  it('shows "No limit" for a missing limit and a real remaining amount for zero', async () => {
    envelopes = [
      envelope({}),
      envelope({
        accountId: 'a2',
        name: 'Fun',
        limit: '0.00',
        limitRevision: 1,
        remaining: '0.00',
        isOverspent: false,
      }),
    ];
    renderPage();
    await screen.findByText('Fun');
    expect(screen.getByText(/No limit/)).toBeInTheDocument();
    expect(screen.getByText('0.00 PLN left')).toBeInTheDocument();
    expect(screen.getByText(/Limit 0\.00 PLN/)).toBeInTheDocument();
  });

  it('says "Over the limit by" in words, not just color, and never hides the add button', async () => {
    envelopes = [
      envelope({
        spent: '5.00',
        limit: '0.00',
        limitRevision: 1,
        remaining: '-5.00',
        isOverspent: true,
      }),
    ];
    renderPage();
    await screen.findByText('Over the limit by 5.00 PLN');
    expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled();
  });

  it('offers no limit control on an archived envelope', async () => {
    envelopes = [envelope({ isArchived: true, spent: '1.00' })];
    renderPage();
    await screen.findByText('Archived');
    expect(screen.queryByRole('button', { name: /limit for/i })).not.toBeInTheDocument();
  });

  it('moves between months, across the year end, and asks the server for that month', async () => {
    renderPage();
    await screen.findByText('12.34 PLN');
    await userEvent.click(screen.getByRole('button', { name: 'Previous month' }));
    await waitFor(() => {
      expect(summaryParams.at(-1)?.get('month')).toBe(shiftMonth(currentMonth(), -1));
    });
    await userEvent.click(screen.getByRole('button', { name: 'Next month' }));
    await userEvent.click(screen.getByRole('button', { name: 'Next month' }));
    await waitFor(() => {
      expect(summaryParams.at(-1)?.get('month')).toBe(shiftMonth(currentMonth(), 1));
    });
  });

  it('switches to my spending and can look at a managed member', async () => {
    renderPage();
    await screen.findByText('12.34 PLN');
    await userEvent.click(screen.getByRole('button', { name: 'My spending' }));
    await waitFor(() => {
      expect(summaryParams.at(-1)?.get('scope')).toBe('personal');
    });
    expect(summaryParams.at(-1)?.get('ownerPersonId')).toBeNull();
    await userEvent.selectOptions(screen.getByLabelText('Whose spending'), 'kid');
    await waitFor(() => {
      expect(summaryParams.at(-1)?.get('ownerPersonId')).toBe('kid');
    });
  });

  it('gives a child only their own spending, with no shared scope', async () => {
    household.myRole = 'Child';
    renderPage();
    await screen.findByText('12.34 PLN');
    expect(screen.queryByRole('button', { name: 'Shared spending' })).not.toBeInTheDocument();
    expect(summaryParams[0]?.get('scope')).toBe('personal');
    expect(screen.getByText(/^My spending ·/)).toBeInTheDocument();
  });

  it('offers retry when the summary fails, not a zero total', async () => {
    summaryStatus = 500;
    renderPage();
    await screen.findByText(/Could not load Budget/);
    expect(screen.queryByText('0.00 PLN')).not.toBeInTheDocument();
    summaryStatus = 200;
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await screen.findByText('12.34 PLN');
  });
});

describe('Limit dialog', () => {
  it('sets a first limit of zero without a revision', async () => {
    const put = vi.fn();
    server.use(
      http.put(`${BASE}/api/budget/accounts/a1/limits/${currentMonth()}`, async ({ request }) => {
        put(await request.json());
        return HttpResponse.json({
          accountId: 'a1',
          month: currentMonth(),
          amount: '0.00',
          revision: 1,
        });
      }),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Set limit for Everyday' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText('Limit (PLN)'), '0');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));
    await waitFor(() => {
      expect(put).toHaveBeenCalledWith({ amount: '0', expectedRevision: null });
    });
  });

  it('validates the amount, accepting a decimal comma', async () => {
    const put = vi.fn();
    server.use(
      http.put(`${BASE}/api/budget/accounts/a1/limits/${currentMonth()}`, async ({ request }) => {
        put(await request.json());
        return HttpResponse.json({
          accountId: 'a1',
          month: currentMonth(),
          amount: '12.50',
          revision: 1,
        });
      }),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Set limit for Everyday' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText('Limit (PLN)'), '1.005');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));
    await within(dialog).findByText('Enter an amount from 0 with at most two decimals.');
    expect(put).not.toHaveBeenCalled();
    await userEvent.clear(within(dialog).getByLabelText('Limit (PLN)'));
    await userEvent.type(within(dialog).getByLabelText('Limit (PLN)'), '12,50');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));
    await waitFor(() => {
      expect(put).toHaveBeenCalledWith({ amount: '12.50', expectedRevision: null });
    });
  });

  it('changes an existing limit with its revision and can clear it', async () => {
    envelopes = [envelope({ spent: '3.00', limit: '10.00', limitRevision: 4, remaining: '7.00' })];
    const put = vi.fn();
    const cleared = vi.fn();
    server.use(
      http.put(`${BASE}/api/budget/accounts/a1/limits/${currentMonth()}`, async ({ request }) => {
        put(await request.json());
        return HttpResponse.json({
          accountId: 'a1',
          month: currentMonth(),
          amount: '20.00',
          revision: 5,
        });
      }),
      http.delete(`${BASE}/api/budget/accounts/a1/limits/${currentMonth()}`, ({ request }) => {
        cleared(new URL(request.url).searchParams.get('expectedRevision'));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Change limit for Everyday' }));
    let dialog = await screen.findByRole('dialog');
    const amount = within(dialog).getByLabelText('Limit (PLN)');
    expect(amount).toHaveValue('10.00');
    await userEvent.clear(amount);
    await userEvent.type(amount, '20');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));
    await waitFor(() => {
      expect(put).toHaveBeenCalledWith({ amount: '20', expectedRevision: 4 });
    });

    await userEvent.click(await screen.findByRole('button', { name: 'Change limit for Everyday' }));
    dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Clear limit' }));
    await waitFor(() => {
      expect(cleared).toHaveBeenCalledWith('4');
    });
  });

  it('offers reload on a revision conflict', async () => {
    envelopes = [envelope({ limit: '10.00', limitRevision: 4, remaining: '10.00' })];
    server.use(
      http.put(
        `${BASE}/api/budget/accounts/a1/limits/${currentMonth()}`,
        () => new HttpResponse(null, { status: 409 }),
      ),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Change limit for Everyday' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));
    await within(dialog).findByText(/Someone else changed this limit/);
    await userEvent.click(within(dialog).getByRole('button', { name: 'Reload' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});
