import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { investmentsModule } from '../index';
import { todayInWarsaw } from '../lib/dates';

import InvestmentsPage from './InvestmentsPage';

import type { Wallet } from '../types';
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
const xtb: Wallet = {
  id: 'w1',
  name: 'XTB main',
  institution: 'XTB',
  kind: 'Brokerage',
  currency: 'PLN',
  visibility: 'Shared',
  ownerPersonId: 'me',
  ownerName: 'Alex',
  isArchived: false,
  version: 7,
  createdAt: '2026-10-02T10:00:00Z',
  canChangeVisibility: true,
  latestBalance: '12450.10',
  latestAsOf: '2026-09-30',
  latestBalancePln: '12450.10',
  rate: null,
  rateMissing: false,
};
const ike: Wallet = {
  ...xtb,
  id: 'w2',
  name: 'Retirement',
  kind: 'IKE',
  currency: 'EUR',
  latestBalance: null,
  latestAsOf: null,
  latestBalancePln: null,
};
const kidSavings: Wallet = {
  ...xtb,
  id: 'w3',
  name: 'Sam savings',
  kind: 'Savings',
  visibility: 'Private',
  ownerPersonId: 'kid',
  ownerName: 'Sam',
};
const beaShared: Wallet = {
  ...xtb,
  id: 'w4',
  name: 'Joint deposit',
  kind: 'Savings',
  ownerPersonId: 'bea',
  ownerName: 'Bea',
  canChangeVisibility: false,
};
const oldDeposit: Wallet = { ...xtb, id: 'w5', name: 'Old deposit', isArchived: true };

let client: QueryClient;
let wallets: Wallet[];
let netWorth: Record<string, unknown>;
let netWorthScopes: string[];

const year = Number(todayInWarsaw().slice(0, 4));
const history = [
  { date: `${String(year - 1)}-11-30`, total: '90000.00', incomplete: false },
  { date: `${String(year - 1)}-12-31`, total: '100000.00', incomplete: false },
  { date: todayInWarsaw(), total: '112500.00', incomplete: false },
];

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <HouseholdStateContext value={{ ...household }}>
        <ToastProvider>
          <MemoryRouter>
            <InvestmentsPage />
          </MemoryRouter>
        </ToastProvider>
      </HouseholdStateContext>
    </Wrapper>,
  );
}

async function openEdit(name: string) {
  await userEvent.click(await screen.findByRole('button', { name: `Actions for ${name}` }));
  await userEvent.click(await screen.findByRole('menuitem', { name: 'Edit' }));
  return screen.findByRole('dialog');
}

beforeEach(() => {
  initI18n([investmentsModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.myRole = 'Owner';
  wallets = [xtb, ike, kidSavings, beaShared, oldDeposit];
  netWorthScopes = [];
  netWorth = {
    asOf: todayInWarsaw(),
    scope: 'household',
    total: '112500.00',
    incomplete: false,
    byKind: [
      { kind: 'Brokerage', total: '90000.00' },
      { kind: 'IKE', total: '22500.00' },
    ],
    byOwner: [],
    wallets: [],
    ratesUsed: [],
  };
  server.use(
    http.get(`${BASE}/api/investments/net-worth`, ({ request }) => {
      const scope = new URL(request.url).searchParams.get('scope') ?? '';
      netWorthScopes.push(scope);
      return HttpResponse.json({ ...netWorth, scope });
    }),
    http.get(`${BASE}/api/investments/net-worth/history`, () =>
      HttpResponse.json({ scope: 'household', points: history }),
    ),
    http.get(`${BASE}/api/investments/wallets`, () =>
      HttpResponse.json({ items: wallets, totalCount: wallets.length, page: 1, pageSize: 100 }),
    ),
  );
});

describe('investmentsModule', () => {
  it('exposes the net worth nav item and an index route under its base path', () => {
    expect(investmentsModule.basePath).toBe('/investments');
    expect(investmentsModule.navItems.map((item) => item.href)).toEqual(['/investments']);
    expect(investmentsModule.routes[0]?.index).toBe(true);
  });
});

describe('InvestmentsPage net worth', () => {
  it('shows the household total and its change since 1 January', async () => {
    renderPage();

    expect(await screen.findByTestId('net-worth-total')).toHaveTextContent(/112.500.00 PLN/);
    expect(
      await screen.findByText(/\+12.500.00 PLN \(\+12\.5%\) since 1 January/),
    ).toBeInTheDocument();
    expect(screen.getByText('Household net worth')).toBeInTheDocument();
  });

  it('switches to my net worth', async () => {
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Mine' }));

    expect(await screen.findByText('My net worth')).toBeInTheDocument();
    expect(netWorthScopes).toContain('mine');
    expect(screen.getByRole('button', { name: 'Mine' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('names the wallets left out for lack of a rate', async () => {
    netWorth = {
      ...netWorth,
      incomplete: true,
      wallets: [
        {
          id: 'w2',
          name: 'Retirement',
          kind: 'IKE',
          currency: 'EUR',
          balance: '10.00',
          balanceAsOf: '2026-09-01',
          balancePln: null,
          rateMissing: true,
        },
      ],
    };
    renderPage();

    expect(
      await screen.findByText(/Not included for lack of an NBP exchange rate: Retirement/),
    ).toBeInTheDocument();
  });

  it('breaks the total down by kind with shares', async () => {
    renderPage();

    const breakdown = await screen.findByRole('region', { name: 'By kind' });
    expect(within(breakdown).getByText('80%')).toBeInTheDocument();
    expect(within(breakdown).getByText('20%')).toBeInTheDocument();
  });

  it('flags a wallet whose latest balance is older than 45 days', async () => {
    // Day arithmetic on the Warsaw calendar date, so a DST change inside the window or a run near
    // midnight cannot turn 46 days into 45.
    const old = new Date(`${todayInWarsaw()}T00:00:00Z`);
    old.setUTCDate(old.getUTCDate() - 46);
    wallets = [
      { ...xtb, latestAsOf: old.toISOString().slice(0, 10) },
      { ...ike, latestAsOf: todayInWarsaw() },
    ];
    renderPage();

    expect(await screen.findByText('Updated 46 days ago')).toBeInTheDocument();
    expect(screen.getAllByText(/Updated \d+ days? ago/)).toHaveLength(1);
  });
});

describe('InvestmentsPage', () => {
  it('explains wallets and offers to add one when there are none', async () => {
    wallets = [];
    renderPage();

    expect(await screen.findByText('No wallets yet')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add wallet' })).toBeInTheDocument();
  });

  it('groups active wallets by kind with a count, and keeps archived ones apart', async () => {
    renderPage();

    const savings = await screen.findByRole('region', { name: 'Savings · 2' });
    expect(within(savings).getByText('Sam savings')).toBeInTheDocument();
    expect(within(savings).getByText('Private')).toBeInTheDocument();
    expect(within(savings).getByText(/Owner: Sam/)).toBeInTheDocument();
    expect(within(savings).getByText(/Owner: Bea/)).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Brokerage · 1' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'IKE · 1' })).toBeInTheDocument();
    expect(screen.getByText('Archived · 1')).toBeInTheDocument();
  });

  it('links each wallet to its detail page and shows its latest balance', async () => {
    renderPage();

    const link = await screen.findByRole('link', { name: 'XTB main' });
    expect(link).toHaveAttribute('href', '/investments/wallets/w1');
    const row = link.closest('li');
    if (!row) throw new Error('wallet row not found');
    expect(within(row).getByText(/12.450.10 PLN/)).toBeInTheDocument();
    const ikeRow = screen.getByRole('link', { name: 'Retirement' }).closest('li');
    if (!ikeRow) throw new Error('wallet row not found');
    expect(within(ikeRow).getByText('No balance yet')).toBeInTheDocument();
  });

  it('creates a wallet for a managed member, never for another adult', async () => {
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/investments/wallets`, async ({ request }) => {
        create(await request.json());
        return HttpResponse.json({ ...kidSavings, id: 'new' }, { status: 201 });
      }),
    );
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Add wallet' }));
    const owner = screen.getByLabelText('Owner');
    expect([...owner.querySelectorAll('option')].map((o) => o.textContent)).toEqual(['You', 'Sam']);
    await userEvent.type(screen.getByLabelText('Name'), 'Sam PPK');
    await userEvent.selectOptions(screen.getByLabelText('Kind'), 'PPK');
    await userEvent.selectOptions(screen.getByLabelText('Institution'), 'Allianz');
    await userEvent.selectOptions(owner, 'kid');
    await userEvent.selectOptions(screen.getByLabelText('Who can see it'), 'Private');
    expect(screen.getByText(/visible only to their owner/)).toBeVisible();
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(create).toHaveBeenCalledWith({
        name: 'Sam PPK',
        institution: 'Allianz',
        kind: 'PPK',
        currency: 'PLN',
        visibility: 'Private',
        ownerPersonId: 'kid',
      });
    });
  });

  it('lets a child add a wallet only for themselves, private by default', async () => {
    household.myRole = 'Child';
    wallets = [];
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Add wallet' }));

    const owner = screen.getByLabelText('Owner');
    expect([...owner.querySelectorAll('option')].map((o) => o.textContent)).toEqual(['You']);
    expect(screen.getByLabelText('Who can see it')).toHaveValue('Private');
  });

  it('validates the name before sending', async () => {
    renderPage();

    await userEvent.click(await screen.findByRole('button', { name: 'Add wallet' }));
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Enter a name.')).toBeInTheDocument();
  });

  it('edits name and visibility with the version; owner, institution, kind and currency are read-only', async () => {
    const update = vi.fn();
    server.use(
      http.put(`${BASE}/api/investments/wallets/w2`, async ({ request }) => {
        update(await request.json());
        return HttpResponse.json({ ...ike, name: 'IKE', version: 8 });
      }),
    );
    renderPage();

    const dialog = await openEdit('Retirement');
    for (const label of ['Owner', 'Institution', 'Kind', 'Currency'])
      expect(within(dialog).getByLabelText(label)).toBeDisabled();
    expect(within(dialog).getByLabelText('Kind')).toHaveValue('IKE');
    const name = within(dialog).getByLabelText('Name');
    await userEvent.clear(name);
    await userEvent.type(name, 'IKE');
    await userEvent.selectOptions(within(dialog).getByLabelText('Who can see it'), 'Private');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(update).toHaveBeenCalledWith({ name: 'IKE', visibility: 'Private', version: 7 });
    });
  });

  it("keeps visibility read-only on another adult's shared wallet", async () => {
    const update = vi.fn();
    server.use(
      http.put(`${BASE}/api/investments/wallets/w4`, async ({ request }) => {
        update(await request.json());
        return HttpResponse.json(beaShared);
      }),
    );
    renderPage();

    const dialog = await openEdit('Joint deposit');
    expect(within(dialog).getByLabelText('Who can see it')).toBeDisabled();
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(update).toHaveBeenCalledWith({
        name: 'Joint deposit',
        visibility: 'Shared',
        version: 7,
      });
    });
  });

  it('explains a conflict when the wallet changed elsewhere', async () => {
    server.use(
      http.put(`${BASE}/api/investments/wallets/w1`, () =>
        HttpResponse.json({ title: 'Conflict' }, { status: 409 }),
      ),
    );
    renderPage();

    const dialog = await openEdit('XTB main');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save' }));

    expect(await within(dialog).findByText(/Someone else changed this wallet/)).toBeInTheDocument();
  });

  it('restores an archived wallet', async () => {
    const restore = vi.fn();
    server.use(
      http.post(`${BASE}/api/investments/wallets/w5/restore`, () => {
        restore();
        return HttpResponse.json({ ...oldDeposit, isArchived: false });
      }),
    );
    renderPage();

    await userEvent.click(await screen.findByText('Archived · 1'));
    await userEvent.click(screen.getByRole('button', { name: 'Restore Old deposit' }));

    await waitFor(() => {
      expect(restore).toHaveBeenCalledOnce();
    });
  });
});
