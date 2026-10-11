import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { investmentsModule } from '../index';

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
};
const ike: Wallet = { ...xtb, id: 'w2', name: 'Retirement', kind: 'IKE', currency: 'EUR' };
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

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <HouseholdStateContext value={{ ...household }}>
        <ToastProvider>
          <InvestmentsPage />
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
  server.use(
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
