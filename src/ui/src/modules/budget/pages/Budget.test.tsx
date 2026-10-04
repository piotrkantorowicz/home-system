import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetLayout } from '../components/BudgetLayout';
import { budgetModule } from '../index';
import { currentMonth } from '../lib/dates';

import BudgetPage from './BudgetPage';
import EnvelopesPage from './EnvelopesPage';

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
    { personId: 'bea', displayName: 'Bea', role: 'Adult', isManaged: false },
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
const shared = {
  id: 'a1',
  name: 'Everyday',
  visibility: 'Household',
  ownerPersonId: null,
  isArchived: false,
  revision: 1,
  createdAt: '2026-10-02T10:00:00Z',
};
const personal = { ...shared, id: 'a2', name: 'Mine', visibility: 'Personal', ownerPersonId: 'me' };
const holidays = { ...shared, id: 'a4', name: 'Holidays' };
const archived = { ...shared, id: 'a3', name: 'Old trip', isArchived: true, revision: 3 };

const envelopeSummary = (
  account: Pick<typeof shared, 'id' | 'name' | 'visibility' | 'isArchived'> & {
    ownerPersonId: string | null;
  },
  count: number,
  limit: string | null,
) => ({
  accountId: account.id,
  name: account.name,
  visibility: account.visibility,
  ownerPersonId: account.ownerPersonId,
  isArchived: account.isArchived,
  spent: '10.00',
  expenseCount: count,
  limit,
  limitRevision: limit === null ? null : 2,
  remaining: null,
  isOverspent: false,
});
let summaries: Record<string, unknown[]>;
let summaryParams: URLSearchParams[];

let client: QueryClient;
let budget: { id: string; currency: string } | null;
let accounts: (Omit<typeof shared, 'ownerPersonId'> & { ownerPersonId: string | null })[];

function ui(path: string) {
  return (
    <HouseholdStateContext value={{ ...household }}>
      <ToastProvider>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/budget" element={<BudgetLayout />}>
              <Route index element={<BudgetPage />} />
              <Route path="envelopes" element={<EnvelopesPage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </HouseholdStateContext>
  );
}

function renderAt(path = '/budget') {
  const Wrapper = createWrapper(client);
  const view = render(<Wrapper>{ui(path)}</Wrapper>);
  return {
    ...view,
    rerenderAt: (next: string) => {
      view.rerender(<Wrapper>{ui(next)}</Wrapper>);
    },
  };
}

beforeEach(() => {
  initI18n([budgetModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.household = { id: 'h1' };
  household.myRole = 'Owner';
  budget = { id: 'b1', currency: 'PLN' };
  accounts = [shared, holidays, personal, archived];
  summaries = {
    shared: [envelopeSummary(shared, 3, '3000.00'), envelopeSummary(holidays, 0, null)],
    personal: [envelopeSummary(personal, 1, '0.00')],
  };
  summaryParams = [];
  server.use(
    http.get(`${BASE}/api/budget`, () =>
      budget ? HttpResponse.json(budget) : new HttpResponse(null, { status: 404 }),
    ),
    http.get(`${BASE}/api/budget/summary`, ({ request }) => {
      const params = new URL(request.url).searchParams;
      summaryParams.push(params);
      return HttpResponse.json({
        month: params.get('month'),
        scope: params.get('scope'),
        currency: 'PLN',
        totalSpent: '0.00',
        expenseCount: 0,
        envelopes: summaries[params.get('scope') ?? 'shared'] ?? [],
        categories: [],
      });
    }),
    http.get(`${BASE}/api/budget/expenses`, () =>
      HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 5 }),
    ),
    http.get(`${BASE}/api/budget/accounts`, () =>
      HttpResponse.json({ items: accounts, totalCount: accounts.length, page: 1, pageSize: 100 }),
    ),
  );
});

describe('Budget gate', () => {
  it('lets an adult choose a currency and set up Budget', async () => {
    budget = null;
    const initialize = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget`, async ({ request }) => {
        initialize(await request.json());
        budget = { id: 'b1', currency: 'EUR' };
        return HttpResponse.json(budget, { status: 201 });
      }),
    );
    renderAt();
    await screen.findByRole('heading', { name: 'Set up Budget' });
    await userEvent.selectOptions(screen.getByLabelText('Currency'), 'EUR');
    await userEvent.click(screen.getByRole('button', { name: 'Set up Budget' }));
    await screen.findByRole('heading', { name: 'Envelopes' });
    expect(initialize).toHaveBeenCalledWith({ currency: 'EUR' });
  });

  it('tells a child to ask an adult when Budget is not set up', async () => {
    household.myRole = 'Child';
    budget = null;
    renderAt();
    await screen.findByRole('heading', { name: 'Ask an adult to set up Budget' });
    expect(screen.queryByRole('button', { name: 'Set up Budget' })).not.toBeInTheDocument();
  });

  it('shows a guest the unavailable state without asking the server for data', async () => {
    household.myRole = 'Guest';
    const requested = vi.fn();
    server.use(
      http.get(`${BASE}/api/budget`, () => {
        requested();
        return HttpResponse.json(budget);
      }),
    );
    renderAt();
    await screen.findByRole('heading', { name: 'Budget is not available' });
    expect(requested).not.toHaveBeenCalled();
  });

  it('shows the unavailable state when the server says forbidden', async () => {
    server.use(http.get(`${BASE}/api/budget`, () => new HttpResponse(null, { status: 403 })));
    renderAt();
    await screen.findByRole('heading', { name: 'Budget is not available' });
    expect(screen.queryByText('Everyday')).not.toBeInTheDocument();
  });

  it('offers retry when the lookup fails, not an empty or setup state', async () => {
    server.use(http.get(`${BASE}/api/budget`, () => new HttpResponse(null, { status: 500 })));
    renderAt();
    await screen.findByText(/Could not load Budget/);
    expect(screen.queryByRole('heading', { name: 'Set up Budget' })).not.toBeInTheDocument();

    server.use(http.get(`${BASE}/api/budget`, () => HttpResponse.json(budget)));
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await screen.findByRole('heading', { name: 'Envelopes' });
  });

  it('drops cached Budget data when the household changes', async () => {
    const view = renderAt();
    await screen.findByRole('heading', { name: 'Envelopes' });
    expect(client.getQueryCache().findAll({ queryKey: ['budget'] }).length).toBeGreaterThan(0);

    household.household = { id: 'h2' };
    view.rerenderAt('/budget');

    await waitFor(() => {
      const keys = client
        .getQueryCache()
        .findAll({ queryKey: ['budget'] })
        .map((q) => q.queryKey[3]);
      expect(keys).not.toContain('h1');
    });
  });
});

describe('Envelopes', () => {
  const openMenu = async (name: string, item: string) => {
    await userEvent.click(await screen.findByRole('button', { name: `Actions for ${name}` }));
    await userEvent.click(await screen.findByRole('menuitem', { name: item }));
  };

  it('groups envelopes into Shared and Personal sections with a count each', async () => {
    renderAt('/budget/envelopes');
    await screen.findByRole('heading', { level: 1, name: 'Envelopes' });
    await screen.findByRole('heading', { name: 'Shared · 2' });
    expect(screen.getByRole('heading', { name: 'Personal · 1' })).toBeInTheDocument();
    expect(screen.getByText('Owners and adults can see these')).toBeInTheDocument();
    expect(screen.getByText('Only you can see these')).toBeInTheDocument();
  });

  it("shows this month's expense count and limit from the server summary", async () => {
    renderAt('/budget/envelopes');
    await screen.findByText('Everyday');
    expect(await screen.findByText(/3 expenses in /)).toBeInTheDocument();
    expect(screen.getByText(/3.?000\.00 \/ month/)).toBeInTheDocument();
    // The request is for the current month and each scope, never a client-side recount.
    const scopes = summaryParams
      .map((p) => `${p.get('scope') ?? ''} ${p.get('month') ?? ''}`)
      .sort();
    expect(scopes).toEqual([`personal ${currentMonth()}`, `shared ${currentMonth()}`]);
  });

  it('tells a zero limit from no limit', async () => {
    renderAt('/budget/envelopes');
    await screen.findByText(/· 0\.00 \/ month/);
    expect(screen.getByText(/1 expense in /)).toBeInTheDocument();
    expect(screen.getAllByText(/No limit/)).toHaveLength(1);
  });

  it('keeps archived envelopes in a collapsed section where they can be restored', async () => {
    renderAt('/budget/envelopes');
    const summary = await screen.findByText('Archived · 1');
    expect(summary.closest('details')).not.toHaveAttribute('open');
    expect(screen.getByText('Old trip')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Restore Old trip' })).toBeInTheDocument();
  });

  it('sets a limit from the menu', async () => {
    renderAt('/budget/envelopes');
    await openMenu('Holidays', 'Set limit');
    await screen.findByRole('heading', { name: /Limit for Holidays/ });
  });

  it('shows personal envelopes of managed members with their owner, and a softer privacy note', async () => {
    accounts = [shared, { ...personal, id: 'a5', name: 'Sam allowance', ownerPersonId: 'kid' }];
    summaries = { shared: [], personal: [] };
    renderAt('/budget/envelopes');
    await screen.findByText('Sam allowance');
    expect(screen.getByText(/Personal · Sam/)).toBeInTheDocument();
    expect(screen.queryByText('Only you can see these')).not.toBeInTheDocument();
  });

  it('creates a shared envelope and explains who can see it', async () => {
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/accounts`, async ({ request }) => {
        create(await request.json());
        return HttpResponse.json({ ...shared, id: 'new', name: 'Holidays' }, { status: 201 });
      }),
    );
    renderAt('/budget/envelopes');
    await userEvent.click(await screen.findByRole('button', { name: 'New envelope' }));
    expect(
      screen.getByText(/Shared envelopes are visible to the household's owners/),
    ).toBeVisible();
    await userEvent.type(screen.getByLabelText('Name'), 'Holidays');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    await waitFor(() => {
      expect(create).toHaveBeenCalledWith({
        name: 'Holidays',
        visibility: 'Household',
        ownerPersonId: null,
      });
    });
  });

  it('lets an adult open a personal envelope for themselves or a managed member only', async () => {
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/accounts`, async ({ request }) => {
        create(await request.json());
        return HttpResponse.json({ ...personal, id: 'new' }, { status: 201 });
      }),
    );
    renderAt('/budget/envelopes');
    await userEvent.click(await screen.findByRole('button', { name: 'New envelope' }));
    await userEvent.selectOptions(screen.getByLabelText('Who can see it'), 'Personal');
    expect(screen.getByText(/private to its owner/)).toBeVisible();
    const owner = screen.getByLabelText('Whose envelope');
    expect([...owner.querySelectorAll('option')].map((o) => o.textContent)).toEqual(['You', 'Sam']);
    await userEvent.selectOptions(owner, 'kid');
    await userEvent.type(screen.getByLabelText('Name'), 'Sam allowance');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    await waitFor(() => {
      expect(create).toHaveBeenCalledWith({
        name: 'Sam allowance',
        visibility: 'Personal',
        ownerPersonId: 'kid',
      });
    });
  });

  it('gives a child only their own personal envelope', async () => {
    household.myRole = 'Child';
    accounts = [personal];
    renderAt('/budget/envelopes');
    await userEvent.click(await screen.findByRole('button', { name: 'New envelope' }));
    expect(screen.queryByLabelText('Who can see it')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Whose envelope')).not.toBeInTheDocument();
    expect(screen.getByText(/private to its owner/)).toBeVisible();
  });

  it('validates the name before sending', async () => {
    renderAt('/budget/envelopes');
    await userEvent.click(await screen.findByRole('button', { name: 'New envelope' }));
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    await screen.findByText('Enter a name.');
  });

  it('renames with the expected revision', async () => {
    const rename = vi.fn();
    server.use(
      http.put(`${BASE}/api/budget/accounts/a1`, async ({ request }) => {
        rename(await request.json());
        return HttpResponse.json({ ...shared, name: 'Daily', revision: 2 });
      }),
    );
    renderAt('/budget/envelopes');
    await openMenu('Everyday', 'Rename');
    const name = screen.getByLabelText('Name');
    await userEvent.clear(name);
    await userEvent.type(name, 'Daily');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));
    await waitFor(() => {
      expect(rename).toHaveBeenCalledWith({ name: 'Daily', expectedRevision: 1 });
    });
  });

  it('archives and restores with the expected revision', async () => {
    const calls: string[] = [];
    server.use(
      http.post(`${BASE}/api/budget/accounts/a1/archive`, async ({ request }) => {
        calls.push(`archive ${JSON.stringify(await request.json())}`);
        return HttpResponse.json({ ...shared, isArchived: true, revision: 2 });
      }),
      http.post(`${BASE}/api/budget/accounts/a3/restore`, async ({ request }) => {
        calls.push(`restore ${JSON.stringify(await request.json())}`);
        return HttpResponse.json({ ...archived, isArchived: false, revision: 4 });
      }),
    );
    renderAt('/budget/envelopes');
    await openMenu('Everyday', 'Archive');
    await userEvent.click(await screen.findByRole('button', { name: 'Restore Old trip' }));
    await waitFor(() => {
      expect(calls).toEqual(['archive {"expectedRevision":1}', 'restore {"expectedRevision":3}']);
    });
  });

  it('explains a revision conflict and refreshes the list', async () => {
    const loads = vi.fn();
    server.use(
      http.get(`${BASE}/api/budget/accounts`, () => {
        loads();
        return HttpResponse.json({ items: accounts, totalCount: 3, page: 1, pageSize: 100 });
      }),
      http.post(
        `${BASE}/api/budget/accounts/a1/archive`,
        () => new HttpResponse(null, { status: 409 }),
      ),
    );
    renderAt('/budget/envelopes');
    await openMenu('Everyday', 'Archive');
    await screen.findByText(/Someone else changed this envelope/);
    await waitFor(() => {
      expect(loads.mock.calls.length).toBeGreaterThan(1);
    });
  });

  it('shows a retryable error when the list fails to load', async () => {
    server.use(
      http.get(`${BASE}/api/budget/accounts`, () => new HttpResponse(null, { status: 500 })),
    );
    renderAt('/budget/envelopes');
    await screen.findByText(/Could not load Budget/);
    expect(screen.queryByText('No envelopes yet')).not.toBeInTheDocument();
  });

  it('distinguishes a genuinely empty list', async () => {
    accounts = [];
    renderAt('/budget/envelopes');
    await screen.findByText('No envelopes yet');
  });
});
