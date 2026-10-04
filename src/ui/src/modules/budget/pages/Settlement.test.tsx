import { ToastProvider } from '@shared/context/ToastContext';
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
  myPersonId: 'a',
  members: [
    { personId: 'a', displayName: 'Alex', role: 'Owner', isManaged: false },
    { personId: 'b', displayName: 'Bea', role: 'Adult', isManaged: false },
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
let repayments: Record<string, unknown>[];
let posted: Record<string, unknown>[];
let voidStatus: number;

function renderPage() {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <ToastProvider>
        <HouseholdStateContext value={{ ...household }}>
          <MemoryRouter initialEntries={['/budget/settlement']}>
            <Routes>
              <Route path="/budget" element={<BudgetLayout />}>
                <Route path="settlement" element={<SettlementPage />} />
              </Route>
            </Routes>
          </MemoryRouter>
        </HouseholdStateContext>
      </ToastProvider>
    </Wrapper>,
  );
}

beforeEach(() => {
  initI18n([budgetModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.myRole = 'Owner';
  household.myPersonId = 'a';
  status = 200;
  requested = 0;
  repayments = [];
  posted = [];
  voidStatus = 200;
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
    http.get(`${BASE}/api/budget/settlements`, () =>
      HttpResponse.json({
        items: repayments,
        totalCount: repayments.length,
        page: 1,
        pageSize: 10,
      }),
    ),
    http.post(`${BASE}/api/budget/settlements`, async ({ request }) => {
      posted.push((await request.json()) as Record<string, unknown>);
      return HttpResponse.json({ repaymentId: 'r1', revision: 1, created: true }, { status: 201 });
    }),
    http.post(`${BASE}/api/budget/settlements/:id/void`, async ({ request }) => {
      posted.push((await request.json()) as Record<string, unknown>);
      return voidStatus === 200
        ? HttpResponse.json({ repaymentId: 'r1', revision: 2, created: false })
        : new HttpResponse(null, { status: voidStatus });
    }),
    http.get(`${BASE}/api/budget/settlement`, () => {
      requested += 1;
      return status === 200 ? HttpResponse.json(body) : new HttpResponse(null, { status });
    }),
  );
});

const SETTLED = { currency: 'PLN', isSettled: true, balances: [], suggestions: [] };
const transfer = (from: string, fromName: string, to: string, toName: string, amount: string) => ({
  fromPersonId: from,
  fromDisplayName: fromName,
  toPersonId: to,
  toDisplayName: toName,
  amount,
});

describe('Settle up', () => {
  it('answers first: who owes whom, with the amount and the no-money note', async () => {
    renderPage();
    await screen.findByRole('heading', { level: 1, name: 'Settle up' });
    expect(await screen.findByText('Bea owes you')).toBeInTheDocument();
    expect(screen.getByText(/Shared costs one person paid for/)).toBeInTheDocument();
    expect(screen.getByText(/no money moves from here/)).toBeInTheDocument();
    expect(screen.queryByText(/Outstanding balance/)).not.toBeInTheDocument();
  });

  it('shows neutral balances around zero for each person', async () => {
    renderPage();
    await screen.findByRole('heading', { name: 'Balances' });
    expect(screen.getByText(/is owed/)).toBeInTheDocument();
    expect(screen.getByText(/^owes/)).toBeInTheDocument();
  });

  it('keeps the explanation in a collapsed details block', async () => {
    renderPage();
    const how = await screen.findByText('How this is worked out');
    expect(how.closest('details')).not.toHaveAttribute('open');
    expect(screen.getByText(/Future-dated entries count immediately/)).toBeInTheDocument();
    expect(screen.getByText(/never cleared automatically/)).toBeInTheDocument();
  });

  it('only mentions simplified recipients when three or more people are involved', async () => {
    renderPage();
    await screen.findByText('Bea owes you');
    expect(screen.queryByText(/suggested recipient can differ/)).not.toBeInTheDocument();
  });

  it('with three people shows a card per suggestion that names the actual payer and payee', async () => {
    body = {
      currency: 'PLN',
      isSettled: false,
      balances: [
        person('a', 'Alex', '60.00'),
        person('b', 'Bea', '-30.00', true),
        person('c', 'Cy', '-30.00'),
      ],
      suggestions: [
        transfer('b', 'Bea', 'a', 'Alex', '30.00'),
        transfer('c', 'Cy', 'a', 'Alex', '30.00'),
      ],
    };
    renderPage();
    await screen.findByText(/suggested recipient can differ/);
    expect(screen.getAllByRole('button', { name: /^Record: / })).toHaveLength(2);
    expect(screen.getByRole('button', { name: 'Record: Cy paid Alex' })).toBeInTheDocument();
    expect(screen.getByText('Former adult')).toBeInTheDocument();
  });

  it('shows a distinct settled card when nothing is owed, with no undo to offer', async () => {
    body = SETTLED;
    renderPage();
    await screen.findByText("You're all settled up");
    expect(screen.queryByText('Balances')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Undo' })).not.toBeInTheDocument();
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
    expect(screen.queryByText("You're all settled up")).not.toBeInTheDocument();
    status = 200;
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await screen.findByText('Bea owes you');
  });

  describe('repayments', () => {
    const repayment = (over: Record<string, unknown> = {}) => ({
      id: 'r1',
      fromPersonId: 'b',
      fromDisplayName: 'Bea',
      toPersonId: 'a',
      toDisplayName: 'Alex',
      amount: '15.00',
      paidOn: '2026-05-01',
      note: 'Cash',
      addedByPersonId: 'a',
      addedByDisplayName: 'Alex',
      revision: 1,
      isVoided: false,
      voidedAt: null,
      voidReason: null,
      createdAt: '2026-05-01T10:00:00Z',
      ...over,
    });
    // Alex is owed, so the card says "Record: Bea paid Alex"; Bea's own page would say "I've paid".
    const recordPaid = () => screen.findByRole('button', { name: 'Record: Bea paid Alex' });

    it('records the suggestion from a small confirm that says no money moves', async () => {
      renderPage();
      await userEvent.click(await recordPaid());
      expect(screen.getByText(/Bea paid Alex 40\.00 PLN/)).toBeInTheDocument();
      expect(screen.getByText(/doesn't move money/)).toBeInTheDocument();
      expect(screen.queryByText(/more than is currently owed/)).not.toBeInTheDocument();
      expect(screen.queryByText(/balances changed/)).not.toBeInTheDocument();
      await userEvent.click(screen.getByRole('button', { name: 'Confirm payment' }));
      await screen.findByText('Payment recorded.');
      expect(posted[0]).toMatchObject({
        fromPersonId: 'b',
        toPersonId: 'a',
        amount: '40.00',
        note: null,
      });
      expect(posted[0]).toHaveProperty('clientRequestId');
    });

    it('says "I\'ve paid" when the signed-in person is the payer', async () => {
      household.myPersonId = 'b';
      renderPage();
      await screen.findByText('You owe Alex');
      expect(screen.getByRole('button', { name: "I've paid Alex" })).toBeInTheDocument();
      household.myPersonId = 'a';
    });

    it('records a different (partial) amount through the full form', async () => {
      renderPage();
      await screen.findByText('Bea owes you');
      await userEvent.click(screen.getByRole('button', { name: 'Record a different amount' }));
      const amount = await screen.findByLabelText(/Amount/);
      expect(amount).toHaveValue('40.00');
      await userEvent.clear(amount);
      await userEvent.type(amount, '15,50');
      await userEvent.click(screen.getByRole('button', { name: 'Review' }));
      await userEvent.click(screen.getByRole('button', { name: 'Confirm payment' }));
      await screen.findByText('Payment recorded.');
      expect(posted[0]).toMatchObject({ amount: '15.50' });
    });

    it('rejects the same person on both sides and a bad amount before the review step', async () => {
      renderPage();
      await screen.findByText('Bea owes you');
      await userEvent.click(screen.getByRole('button', { name: 'Record payment' }));
      await userEvent.selectOptions(await screen.findByLabelText('Paid to'), 'a');
      await userEvent.type(screen.getByLabelText(/Amount/), '0');
      await userEvent.click(screen.getByRole('button', { name: 'Review' }));
      expect(screen.getByText('Choose two different people.')).toBeInTheDocument();
      expect(screen.getByText(/positive amount/)).toBeInTheDocument();
      expect(posted).toHaveLength(0);
    });

    it('warns, without blocking, when the payment is more than is owed', async () => {
      renderPage();
      await screen.findByText('Bea owes you');
      await userEvent.click(screen.getByRole('button', { name: 'Record a different amount' }));
      const amount = await screen.findByLabelText(/Amount/);
      await userEvent.clear(amount);
      await userEvent.type(amount, '40,01');
      await userEvent.click(screen.getByRole('button', { name: 'Review' }));
      expect(screen.getByText(/more than is currently owed/)).toBeInTheDocument();
      await userEvent.click(screen.getByRole('button', { name: 'Confirm payment' }));
      await screen.findByText('Payment recorded.');
      expect(posted[0]).toMatchObject({ amount: '40.01' });
    });

    it('warns when the suggestion changed while the confirm was open', async () => {
      renderPage();
      await userEvent.click(await recordPaid());
      body = { ...body, suggestions: [transfer('b', 'Bea', 'a', 'Alex', '25.00')] };
      await client.invalidateQueries();
      await screen.findByText(/balances changed since this suggestion/);
    });

    it('undoes only the payment just recorded, once everyone is settled', async () => {
      server.use(
        http.post(`${BASE}/api/budget/settlements`, async ({ request }) => {
          posted.push((await request.json()) as Record<string, unknown>);
          body = SETTLED;
          return HttpResponse.json(
            { repaymentId: 'r9', revision: 1, created: true },
            { status: 201 },
          );
        }),
      );
      renderPage();
      await userEvent.click(await recordPaid());
      await userEvent.click(screen.getByRole('button', { name: 'Confirm payment' }));
      await screen.findByText("You're all settled up");
      body = { ...body, isSettled: false };
      await userEvent.click(screen.getByRole('button', { name: 'Undo' }));
      await screen.findByText('Payment undone.');
      expect(posted[1]).toEqual({ expectedRevision: 1, reason: 'Undone right after recording' });
      expect(screen.queryByRole('button', { name: 'Undo' })).not.toBeInTheDocument();
    });

    it('recovers when the undo is stale: error shown, no blind retry', async () => {
      voidStatus = 409;
      renderPage();
      await userEvent.click(await recordPaid());
      await userEvent.click(screen.getByRole('button', { name: 'Confirm payment' }));
      await userEvent.click(await screen.findByRole('button', { name: 'Undo' }));
      await screen.findByText(/Could not undo that payment/);
      expect(screen.queryByRole('button', { name: 'Undo' })).not.toBeInTheDocument();
    });

    it('keeps the history and the record button when everyone is settled', async () => {
      body = SETTLED;
      repayments = [repayment()];
      renderPage();
      await screen.findByText("You're all settled up");
      expect(screen.getByRole('button', { name: 'Record payment' })).toBeInTheDocument();
      expect(await screen.findByText('Bea paid Alex 15.00 PLN on 2026-05-01')).toBeInTheDocument();
    });

    it('shows the latest three payments and expands to the rest', async () => {
      repayments = [1, 2, 3, 4].map((n) =>
        repayment({ id: `r${String(n)}`, amount: `${String(n)}.00` }),
      );
      renderPage();
      await screen.findByText('Bea paid Alex 1.00 PLN on 2026-05-01');
      expect(screen.queryByText('Bea paid Alex 4.00 PLN on 2026-05-01')).not.toBeInTheDocument();
      await userEvent.click(screen.getByRole('button', { name: 'Show all (4)' }));
      await screen.findByText('Bea paid Alex 4.00 PLN on 2026-05-01');
    });

    it('marks voided payments with their reason and offers no second void', async () => {
      repayments = [
        repayment(),
        repayment({ id: 'r2', isVoided: true, voidReason: 'Wrong person', amount: '5.00' }),
      ];
      renderPage();
      await screen.findByText('Bea paid Alex 5.00 PLN on 2026-05-01');
      expect(screen.getByText('Voided')).toBeInTheDocument();
      expect(screen.getByText(/Reason: Wrong person/)).toBeInTheDocument();
      expect(screen.getAllByRole('button', { name: 'Payment actions' })).toHaveLength(1);
    });

    const openVoid = async () => {
      await userEvent.click(await screen.findByRole('button', { name: 'Payment actions' }));
      await userEvent.click(await screen.findByRole('menuitem', { name: 'Void payment' }));
    };

    it('requires a reason to void a payment from the menu, then sends the revision', async () => {
      repayments = [repayment({ revision: 3 })];
      renderPage();
      await openVoid();
      await userEvent.click(screen.getByRole('button', { name: 'Void payment' }));
      expect(screen.getByText('Enter a reason.')).toBeInTheDocument();
      expect(posted).toHaveLength(0);
      await userEvent.type(screen.getByLabelText('Reason'), 'Entered twice');
      await userEvent.click(screen.getByRole('button', { name: 'Void payment' }));
      await screen.findByText('Payment voided.');
      expect(posted[0]).toEqual({ expectedRevision: 3, reason: 'Entered twice' });
    });

    it('explains a stale void instead of failing silently', async () => {
      repayments = [repayment()];
      voidStatus = 409;
      renderPage();
      await openVoid();
      await userEvent.type(screen.getByLabelText('Reason'), 'Oops');
      await userEvent.click(screen.getByRole('button', { name: 'Void payment' }));
      await screen.findByText(/Someone else changed this payment/);
    });
  });
});
