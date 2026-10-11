import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { investmentsModule } from '../index';
import { todayLocal } from '../lib/dates';

import WalletDetailPage from './WalletDetailPage';

import type { Wallet } from '../types';
import type { FakeHouseholdState } from '@/test/utils/householdState';

import { server } from '@/test/mocks/server';
import { HouseholdStateContext } from '@/test/utils/householdState';
import { createWrapper } from '@/test/utils/queryWrapper';

const household: FakeHouseholdState = {
  household: { id: 'h1' },
  myRole: 'Owner',
  myPersonId: 'me',
  members: [{ personId: 'me', displayName: 'Alex', role: 'Owner', isManaged: false }],
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
  version: 3,
  createdAt: '2026-09-01T10:00:00Z',
  canChangeVisibility: true,
  latestBalance: '300.00',
  latestAsOf: '2026-09-01',
};
const history = [
  { asOf: '2026-09-01', amount: '300.00', previousAmount: '350.50', note: 'dip' },
  { asOf: '2026-08-31', amount: '350.50', previousAmount: null, note: null },
].map((s) => ({
  ...s,
  recordedByName: 'Alex',
  recordedAt: '2026-09-01T10:00:00Z',
  updatedAt: '2026-09-01T10:00:00Z',
}));

function rowAt(rows: HTMLElement[], index: number): HTMLElement {
  const row = rows[index];
  if (!row) throw new Error(`no row ${String(index)}`);
  return row;
}

let client: QueryClient;
let wallet: Wallet | null;

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <HouseholdStateContext value={{ ...household }}>
        <ToastProvider>
          <MemoryRouter initialEntries={['/investments/wallets/w1']}>
            <Routes>
              <Route path="/investments/wallets/:id" element={<WalletDetailPage />} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </HouseholdStateContext>
    </Wrapper>,
  );
}

beforeEach(() => {
  initI18n([investmentsModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  wallet = xtb;
  server.use(
    http.get(`${BASE}/api/investments/wallets/w1`, () =>
      wallet ? HttpResponse.json(wallet) : new HttpResponse(null, { status: 404 }),
    ),
    http.get(`${BASE}/api/investments/wallets/w1/snapshots`, () =>
      HttpResponse.json({ items: history, totalCount: history.length, page: 1, pageSize: 20 }),
    ),
  );
});

describe('WalletDetailPage', () => {
  it('shows the latest balance in the wallet currency', async () => {
    renderPage();

    expect(await screen.findByRole('heading', { level: 1, name: 'XTB main' })).toBeInTheDocument();
    expect(screen.getByText('Latest balance').parentElement).toHaveTextContent('300.00 PLN');
  });

  it('says the PLN value is pending for a foreign-currency wallet', async () => {
    wallet = { ...xtb, currency: 'EUR' };
    renderPage();

    expect(await screen.findByText(/PLN value appears once exchange rates/)).toBeInTheDocument();
    const rows = within(await screen.findByRole('table')).getAllByRole('row');
    expect(within(rowAt(rows, 1)).getAllByText('—')).toHaveLength(1);
  });

  it('shows each balance with its change from the previous one', async () => {
    renderPage();

    const rows = within(await screen.findByRole('table')).getAllByRole('row');
    expect(rows[1]).toHaveTextContent(/[−-]50.50 PLN/);
    expect(rows[2]).toHaveTextContent('—');
  });

  it('records a balance typed with a decimal comma for today', async () => {
    const put = vi.fn();
    server.use(
      http.put(
        `${BASE}/api/investments/wallets/w1/snapshots/:asOf`,
        async ({ request, params }) => {
          put(params.asOf, await request.json());
          return HttpResponse.json(
            { asOf: params.asOf, amount: '12450.10', note: null, created: true },
            { status: 201 },
          );
        },
      ),
    );
    renderPage();

    await userEvent.type(await screen.findByLabelText('Balance (PLN)'), '12 450,1');
    await userEvent.click(screen.getByRole('button', { name: 'Save balance' }));

    await waitFor(() => {
      expect(put).toHaveBeenCalledWith(todayLocal(), { amount: '12450.1', note: null });
    });
  });

  it('rejects an amount with more than two decimals before sending', async () => {
    const put = vi.fn();
    server.use(
      http.put(`${BASE}/api/investments/wallets/w1/snapshots/:asOf`, () => {
        put();
        return HttpResponse.json({});
      }),
    );
    renderPage();

    await userEvent.type(await screen.findByLabelText('Balance (PLN)'), '1,234');
    await userEvent.click(screen.getByRole('button', { name: 'Save balance' }));

    expect(await screen.findByText(/at most two decimals/)).toBeInTheDocument();
    expect(put).not.toHaveBeenCalled();
  });

  it('deletes a balance', async () => {
    const remove = vi.fn();
    server.use(
      http.delete(`${BASE}/api/investments/wallets/w1/snapshots/:asOf`, ({ params }) => {
        remove(params.asOf);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();

    const rows = within(await screen.findByRole('table')).getAllByRole('row');
    await userEvent.click(within(rowAt(rows, 2)).getByRole('button', { name: /Delete balance/ }));

    await waitFor(() => {
      expect(remove).toHaveBeenCalledWith('2026-08-31');
    });
  });

  it('shows not-found for a missing or private wallet', async () => {
    wallet = null;
    renderPage();

    expect(await screen.findByText('Wallet not found')).toBeInTheDocument();
  });
});
