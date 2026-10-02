import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetLayout } from '../components/BudgetLayout';
import { budgetModule } from '../index';

import SettlementPage from './SettlementPage';

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
const person = (id: string, name: string, net: string, former = false) => ({
  personId: id,
  displayName: name,
  isFormerAdult: former,
  net,
});

let client: QueryClient;
let body: Record<string, unknown>;
let status: number;
let requested: number;

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <HouseholdStateContext value={{ ...household }}>
        <MemoryRouter initialEntries={['/budget/settlement']}>
          <Routes>
            <Route path="/budget" element={<BudgetLayout />}>
              <Route path="settlement" element={<SettlementPage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </HouseholdStateContext>
    </Wrapper>,
  );
}

beforeEach(() => {
  initI18n([budgetModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.myRole = 'Owner';
  status = 200;
  requested = 0;
  body = {
    currency: 'PLN',
    isSettled: false,
    balances: [person('a', 'Alex', '40.00'), person('b', 'Bea', '-40.00')],
    suggestions: [
      {
        fromPersonId: 'b',
        fromDisplayName: 'Bea',
        toPersonId: 'a',
        toDisplayName: 'Alex',
        amount: '40.00',
      },
    ],
  };
  server.use(
    http.get(`${BASE}/api/budget`, () => HttpResponse.json({ id: 'b1', currency: 'PLN' })),
    http.get(`${BASE}/api/budget/settlement`, () => {
      requested += 1;
      return status === 200 ? HttpResponse.json(body) : new HttpResponse(null, { status });
    }),
  );
});

describe('Settle up', () => {
  it('labels the page as all recorded entries and shows balances and the suggested payment', async () => {
    renderPage();
    await screen.findByRole('heading', { name: 'Outstanding balance — all recorded entries' });
    await screen.findByText('Is owed 40.00 PLN');
    expect(screen.getByText('Owes 40.00 PLN')).toBeInTheDocument();
    expect(screen.getByText('Bea pays Alex 40.00 PLN')).toBeInTheDocument();
    expect(screen.getByText(/proposals only/)).toBeInTheDocument();
  });

  it('explains how balances are worked out, including future-dated and excluded entries', async () => {
    renderPage();
    const how = await screen.findByRole('heading', { name: 'How this is worked out' });
    expect(how).toBeInTheDocument();
    expect(screen.getByText(/Future-dated entries count immediately/)).toBeInTheDocument();
    expect(
      screen.getByText(/Personal envelopes, household funds and voided expenses/),
    ).toBeInTheDocument();
    expect(screen.getByText(/never cleared automatically/)).toBeInTheDocument();
  });

  it('only mentions simplified recipients when three or more people are involved', async () => {
    renderPage();
    await screen.findByText('Bea pays Alex 40.00 PLN');
    expect(screen.queryByText(/suggested recipient can differ/)).not.toBeInTheDocument();
  });

  it('explains simplification for three or more people and marks former adults', async () => {
    body = {
      currency: 'PLN',
      isSettled: false,
      balances: [
        person('a', 'Alex', '60.00'),
        person('b', 'Bea', '-30.00', true),
        person('c', 'Cy', '-30.00'),
      ],
      suggestions: [
        {
          fromPersonId: 'b',
          fromDisplayName: 'Bea',
          toPersonId: 'a',
          toDisplayName: 'Alex',
          amount: '30.00',
        },
        {
          fromPersonId: 'c',
          fromDisplayName: 'Cy',
          toPersonId: 'a',
          toDisplayName: 'Alex',
          amount: '30.00',
        },
      ],
    };
    renderPage();
    await screen.findByText(/suggested recipient can differ/);
    expect(screen.getByText('Former adult')).toBeInTheDocument();
  });

  it('shows a distinct settled state when nothing is owed', async () => {
    body = { currency: 'PLN', isSettled: true, balances: [], suggestions: [] };
    renderPage();
    await screen.findByText('Everyone is settled up');
    expect(screen.queryByText('Suggested payments')).not.toBeInTheDocument();
  });

  it('is unavailable to a child, who never even asks the server', async () => {
    household.myRole = 'Child';
    renderPage();
    await screen.findByRole('heading', { name: 'Settle up is for adults' });
    expect(requested).toBe(0);
  });

  it('shows the unavailable state when the server says forbidden', async () => {
    status = 403;
    renderPage();
    await screen.findByRole('heading', { name: 'Settle up is for adults' });
  });

  it('offers retry on a failed load instead of an empty ledger', async () => {
    status = 500;
    renderPage();
    await screen.findByText(/Could not load Budget/);
    expect(screen.queryByText('Everyone is settled up')).not.toBeInTheDocument();
    status = 200;
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await screen.findByText('Is owed 40.00 PLN');
  });
});
