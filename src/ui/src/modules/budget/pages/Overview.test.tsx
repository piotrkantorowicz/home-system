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
let settlement: { isSettled: boolean; suggestions: Record<string, unknown>[] };
let settlementCalls: number;
let expenseItems: Record<string, unknown>[];

const transfer = (from: string, to: string, toName = 'Ania', amount = '10.00') => ({
  fromPersonId: from,
  fromDisplayName: 'Alex',
  toPersonId: to,
  toDisplayName: toName,
  amount,
});
const expense = (over: Record<string, unknown>) => ({
  id: 'e',
  accountId: 'a1',
  amount: '1.00',
  category: 'Groceries',
  occurredOn: '2026-10-01',
  description: null,
  fundingSource: 'Individual',
  paidByPersonId: 'ania',
  paidByDisplayName: 'Ania',
  addedByPersonId: 'me',
  addedByDisplayName: 'Alex',
  revision: 1,
  createdAt: '2026-10-01T10:00:00Z',
  isVoided: false,
  shares: [{ personId: 'me', personDisplayName: 'Alex', amount: '0.50' }],
  ...over,
});

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
  settlement = { isSettled: true, suggestions: [] };
  settlementCalls = 0;
  expenseItems = [];
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
      HttpResponse.json({
        items: expenseItems,
        totalCount: expenseItems.length,
        page: 1,
        pageSize: 20,
      }),
    ),
    http.get(`${BASE}/api/budget/settlement`, () => {
      settlementCalls += 1;
      return HttpResponse.json({ currency: 'PLN', balances: [], ...settlement });
    }),
    http.get(`${BASE}/api/budget/accounts`, () =>
      HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 100 }),
    ),
  );
});

describe('Overview', () => {
  it('shows the server total, categories and the shared scope', async () => {
    renderPage();
    await screen.findByText('12.34');
    expect(screen.getByRole('radio', { name: 'Shared', checked: true })).toBeInTheDocument();
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
    expect(screen.getAllByText('0.00 left').length).toBeGreaterThan(0);
    expect(screen.getByText('0.00 of 0.00')).toBeInTheDocument();
  });

  it('says "over the limit" in words, exactly, even against a zero limit', async () => {
    envelopes = [
      envelope({
        spent: '0.01',
        limit: '0.00',
        limitRevision: 1,
        remaining: '-0.01',
        isOverspent: true,
      }),
    ];
    renderPage();
    expect((await screen.findAllByText('↑ 0.01 over the limit')).length).toBeGreaterThan(0);
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
    await screen.findByText('12.34');
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

  it('switches to just mine and can look at a managed member', async () => {
    renderPage();
    await screen.findByText('12.34');
    await userEvent.click(screen.getByRole('radio', { name: 'Just mine' }));
    await waitFor(() => {
      expect(summaryParams.at(-1)?.get('scope')).toBe('personal');
    });
    expect(summaryParams.at(-1)?.get('ownerPersonId')).toBeNull();
    await userEvent.selectOptions(screen.getByLabelText('Whose spending'), 'kid');
    await waitFor(() => {
      expect(summaryParams.at(-1)?.get('ownerPersonId')).toBe('kid');
    });
  });

  it('gives a child only their own spending, with no shared scope and no settle card', async () => {
    household.myRole = 'Child';
    settlement = { isSettled: false, suggestions: [transfer('me', 'x')] };
    renderPage();
    await screen.findByText('12.34');
    expect(screen.queryByRole('radio', { name: 'Shared' })).not.toBeInTheDocument();
    expect(summaryParams[0]?.get('scope')).toBe('personal');
    expect(screen.queryByText(/You owe/)).not.toBeInTheDocument();
    expect(settlementCalls).toBe(0);
  });

  it('offers retry when the summary fails, not a zero total', async () => {
    summaryStatus = 500;
    renderPage();
    await screen.findByText(/Could not load Budget/);
    expect(screen.queryByText('0.00')).not.toBeInTheDocument();
    summaryStatus = 200;
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await screen.findByText('12.34');
  });

  it('prompts to settle up when I owe someone, and hides it when just mine', async () => {
    settlement = { isSettled: false, suggestions: [transfer('me', 'ania', 'Ania', '505.73')] };
    renderPage();
    const card = await screen.findByRole('link', { name: /You owe Ania 505\.73/ });
    expect(card).toHaveAttribute('href', '/budget/settlement');
    await userEvent.click(screen.getByRole('radio', { name: 'Just mine' }));
    await waitFor(() => {
      expect(screen.queryByText(/You owe/)).not.toBeInTheDocument();
    });
  });

  it('hides the settle card when everyone is settled', async () => {
    settlement = { isSettled: true, suggestions: [] };
    renderPage();
    await screen.findByText('12.34');
    await waitFor(() => {
      expect(settlementCalls).toBeGreaterThan(0);
    });
    expect(screen.queryByText(/You owe|owes you/)).not.toBeInTheDocument();
  });

  it('lists the latest expenses of the month, description first, only from this scope', async () => {
    expenseItems = [
      expense({ id: 'e1', description: 'Weekly shop', amount: '20.00' }),
      expense({ id: 'e2', description: null, category: 'Transport', amount: '5.00' }),
      expense({ id: 'e3', accountId: 'foreign', description: 'Not in scope', amount: '9.00' }),
    ];
    renderPage();
    await screen.findByText('Weekly shop');
    expect(screen.getByRole('link', { name: /Weekly shop/ })).toHaveAttribute(
      'href',
      '/budget/expenses/e1',
    );
    expect(screen.getAllByText('Transport').length).toBeGreaterThan(0);
    expect(screen.queryByText('Not in scope')).not.toBeInTheDocument();
    expect(screen.getAllByText(/Ania paid · split with you/)).toHaveLength(2);
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
