import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { investmentsModule } from '../index';
import { todayInWarsaw } from '../lib/dates';

import CheckInPage from './CheckInPage';

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
  name: 'XTB IKE',
  institution: 'XTB',
  kind: 'IKE',
  currency: 'PLN',
  visibility: 'Shared',
  ownerPersonId: 'me',
  ownerName: 'Alex',
  isArchived: false,
  version: 1,
  createdAt: '2026-01-01T10:00:00Z',
  canChangeVisibility: true,
  latestBalance: '1000.00',
  latestAsOf: '2026-09-30',
  latestBalancePln: '1000.00',
  rate: null,
  rateMissing: false,
};
const saxo: Wallet = {
  ...xtb,
  id: 'w2',
  name: 'Saxo',
  currency: 'EUR',
  latestBalance: null,
  latestAsOf: null,
};
const closed: Wallet = { ...xtb, id: 'w3', name: 'Closed deposit', isArchived: true };

let client: QueryClient;

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <HouseholdStateContext value={{ ...household }}>
        <ToastProvider>
          <MemoryRouter initialEntries={['/investments/check-in']}>
            <Routes>
              <Route path="/investments/check-in" element={<CheckInPage />} />
              <Route path="/investments" element={<p>Dashboard</p>} />
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
  server.use(
    http.get(`${BASE}/api/investments/wallets`, () =>
      HttpResponse.json({ items: [xtb, saxo, closed], totalCount: 3, page: 1, pageSize: 100 }),
    ),
  );
});

describe('CheckInPage', () => {
  it('lists active wallets only, with their last balance', async () => {
    renderPage();

    expect(await screen.findByLabelText('XTB IKE')).toBeInTheDocument();
    expect(screen.getByLabelText('Saxo')).toBeInTheDocument();
    expect(screen.queryByLabelText('Closed deposit')).not.toBeInTheDocument();
    expect(screen.getByText(/Last: 1.000.00 PLN on/)).toBeInTheDocument();
    expect(screen.getByText('No balance yet')).toBeInTheDocument();
  });

  it('shows the change per row and counts filled rows on the Save button', async () => {
    renderPage();

    const xtbInput = await screen.findByLabelText('XTB IKE');
    expect(screen.getByRole('button', { name: 'Save balances' })).toBeDisabled();
    await userEvent.type(xtbInput, '1250,5');

    expect(screen.getByText(/\+250.50 PLN/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save 1 balance' })).toBeEnabled();
    await userEvent.type(screen.getByLabelText('Saxo'), '10');
    expect(screen.getByRole('button', { name: 'Save 2 balances' })).toBeEnabled();
  });

  it('blocks saving while a row is invalid', async () => {
    renderPage();

    await userEvent.type(await screen.findByLabelText('XTB IKE'), '1.234');

    expect(screen.getByText('Not a valid amount')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Save/ })).toBeDisabled();
  });

  it('sends filled rows only, for one date, then returns to the dashboard', async () => {
    const put = vi.fn();
    server.use(
      http.put(`${BASE}/api/investments/snapshots`, async ({ request }) => {
        put(await request.json());
        return HttpResponse.json({ asOf: todayInWarsaw(), saved: 1, created: 1 });
      }),
    );
    renderPage();

    await userEvent.type(await screen.findByLabelText('Saxo'), '99.9');
    await userEvent.click(screen.getByRole('button', { name: 'Save 1 balance' }));

    expect(await screen.findByText('Dashboard')).toBeInTheDocument();
    expect(put).toHaveBeenCalledWith({
      asOf: todayInWarsaw(),
      entries: [{ walletId: 'w2', amount: '99.9', note: null }],
    });
  });

  it('shows why the server rejected the batch', async () => {
    server.use(
      http.put(`${BASE}/api/investments/snapshots`, () =>
        HttpResponse.json(
          {
            title: 'Business rule violation',
            detail: 'Nothing was saved. Fix these balances: w1: archived',
            errors: { w1: ['archived'] },
          },
          { status: 422 },
        ),
      ),
    );
    renderPage();

    await userEvent.type(await screen.findByLabelText('XTB IKE'), '5');
    await userEvent.click(screen.getByRole('button', { name: 'Save 1 balance' }));

    expect(
      await screen.findByText('Nothing was saved. Fix the balances marked below.'),
    ).toBeInTheDocument();
    expect(screen.getByText('archived')).toBeInTheDocument();
    expect(screen.getByLabelText('XTB IKE')).toHaveAttribute('aria-invalid', 'true');
  });
});
