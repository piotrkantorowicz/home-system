import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { delay, http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetLayout } from '../components/BudgetLayout';
import { budgetModule } from '../index';
import { todayLocal } from '../lib/dates';

import ExpenseDetailPage from './ExpenseDetailPage';
import ExpensesPage from './ExpensesPage';

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
    { personId: 'bea', displayName: 'Bea', role: 'Adult', isManaged: false },
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
// "Save" or "Save 12.50"; not "Save without checking".
const SAVE = /^Save( [\d.]+)?$/;
const account = (id: string, name: string, visibility: string, owner: string | null = null) => ({
  id,
  name,
  visibility,
  ownerPersonId: owner,
  isArchived: false,
  revision: 1,
  createdAt: '2026-10-02T10:00:00Z',
});
const shared = account('a1', 'Everyday', 'Household');
const mine = account('a2', 'Mine', 'Personal', 'me');

const expense = {
  id: 'e1',
  accountId: 'a1',
  amount: '100.00',
  category: 'Groceries',
  occurredOn: '2026-10-01',
  fundingSource: 'Individual',
  paidByPersonId: 'bea',
  paidByDisplayName: 'Bea',
  addedByPersonId: 'me',
  addedByDisplayName: 'Alex',
  revision: 1,
  createdAt: '2026-10-01T10:00:00Z',
  isVoided: false,
  shares: [
    { personId: 'me', personDisplayName: 'Alex', amount: '50.00' },
    { personId: 'bea', personDisplayName: 'Bea', amount: '50.00' },
  ],
  history: [
    {
      revisionNumber: 1,
      operation: 'Create',
      actorPersonId: 'me',
      actorDisplayName: 'Alex',
      reason: null,
      createdAt: '2026-10-01T10:00:00Z',
      snapshot: {
        amount: '100.00',
        category: 'Groceries',
        occurredOn: '2026-10-01',
        fundingSource: 'Individual',
        paidByPersonId: 'bea',
        paidByDisplayName: 'Bea',
        addedByPersonId: 'me',
        addedByDisplayName: 'Alex',
        isVoided: false,
        shares: [
          { personId: 'me', personDisplayName: 'Alex', amount: '50.00' },
          { personId: 'bea', personDisplayName: 'Bea', amount: '50.00' },
        ],
      },
    },
  ],
};

let client: QueryClient;
let listItems: Record<string, unknown>[];
let listStatus: number;
let listParams: URLSearchParams[];

function ui(path: string) {
  return (
    <HouseholdStateContext value={{ ...household }}>
      <ToastProvider>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/budget" element={<BudgetLayout />}>
              <Route path="expenses" element={<ExpensesPage />} />
              <Route path="expenses/:id" element={<ExpenseDetailPage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </HouseholdStateContext>
  );
}

function renderAt(path: string) {
  const Wrapper = createWrapper(client);
  return render(<Wrapper>{ui(path)}</Wrapper>);
}

const page = (items: Record<string, unknown>[]) => ({
  items,
  totalCount: items.length,
  page: 1,
  pageSize: 25,
});

beforeEach(() => {
  initI18n([budgetModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.myRole = 'Owner';
  listItems = [];
  listStatus = 200;
  listParams = [];
  server.use(
    http.get(`${BASE}/api/budget`, () => HttpResponse.json({ id: 'b1', currency: 'PLN' })),
    http.get(`${BASE}/api/budget/accounts`, () =>
      HttpResponse.json({ items: [shared, mine], totalCount: 2, page: 1, pageSize: 100 }),
    ),
    http.get(`${BASE}/api/budget/expenses`, ({ request }) => {
      listParams.push(new URL(request.url).searchParams);
      return listStatus === 200
        ? HttpResponse.json(page(listItems))
        : new HttpResponse(null, { status: listStatus });
    }),
    http.get(`${BASE}/api/budget/expenses/e1`, () => HttpResponse.json(expense)),
  );
});

async function openAddForm() {
  renderAt('/budget/expenses');
  await userEvent.click(await screen.findByRole('button', { name: 'Add expense' }));
  return screen.findByRole('dialog');
}

describe('Add expense', () => {
  it('opens the form from ?add=1 (the phone tab bar action) and drops the flag on close', async () => {
    renderAt('/budget/expenses?add=1');
    const dialog = await screen.findByRole('dialog');
    await userEvent.keyboard('{Escape}');
    expect(dialog).not.toBeInTheDocument();
    await userEvent.click(await screen.findByRole('button', { name: 'Add expense' }));
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
  });

  it('records a shared expense with the caller as recorder and an explicit payer and split', async () => {
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/expenses`, async ({ request }) => {
        create(await request.json());
        return HttpResponse.json({ expenseId: 'new', revision: 1, created: true }, { status: 201 });
      }),
    );
    const dialog = await openAddForm();
    expect(within(dialog).getByText('Recorded by Alex (you)')).toBeVisible();
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '12,50');
    await userEvent.type(within(dialog).getByLabelText('What for'), 'Bus tickets');
    await userEvent.click(within(dialog).getByRole('radio', { name: 'Transport' }));
    await userEvent.click(within(dialog).getByRole('radio', { name: 'Bea' }));
    await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Alex' }));
    await waitFor(() => {
      expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();
    });
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));

    await waitFor(() => {
      expect(create).toHaveBeenCalledTimes(1);
    });
    expect(create.mock.calls[0]?.[0]).toMatchObject({
      accountId: 'a1',
      amount: '12.50',
      occurredOn: todayLocal(),
      category: 'Transport',
      fundingSource: 'Individual',
      paidByPersonId: 'bea',
      participantIds: ['bea'],
      description: 'Bus tickets',
    });
    expect((create.mock.calls[0]?.[0] as { clientRequestId: string }).clientRequestId).toMatch(
      /^[0-9a-f-]{36}$/,
    );
  });

  it('previews each share live in integer cents, remainder to the lowest person id, whatever the tick order', async () => {
    const original = household.members;
    // Ids sort as cy < me < zed; ticking order below is deliberately different.
    household.members = [
      { personId: 'zed', displayName: 'Zed', role: 'Adult', isManaged: false },
      { personId: 'me', displayName: 'Alex', role: 'Owner', isManaged: false },
      { personId: 'cy', displayName: 'Cy', role: 'Adult', isManaged: false },
    ];
    try {
      const dialog = await openAddForm();
      await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '100,00');

      const share = (name: string) =>
        within(
          within(dialog).getByRole('checkbox', { name }).closest('label') as HTMLElement,
        ).getByText(/^\d+\.\d{2}$/).textContent;
      expect(share('Cy')).toBe('33.34');
      expect(share('Alex')).toBe('33.33');
      expect(share('Zed')).toBe('33.33');
      expect(within(dialog).getByRole('button', { name: 'Save 100.00' })).toBeVisible();

      await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Cy' }));
      expect(share('Alex')).toBe('50.00');
      expect(share('Zed')).toBe('50.00');
    } finally {
      household.members = original;
    }
  });

  it('sends household funds with nobody credited and no split', async () => {
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/expenses`, async ({ request }) => {
        create(await request.json());
        return HttpResponse.json({ expenseId: 'new', revision: 1, created: true }, { status: 201 });
      }),
    );
    const dialog = await openAddForm();
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '20');
    await userEvent.click(within(dialog).getByRole('radio', { name: 'Household account' }));
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));

    await waitFor(() => {
      expect(create).toHaveBeenCalledTimes(1);
    });
    expect(create.mock.calls[0]?.[0]).toMatchObject({
      fundingSource: 'HouseholdFunds',
      paidByPersonId: null,
      participantIds: [],
      description: null,
    });
  });

  it('keeps Save outside the scrolling body so it stays reachable on a phone', async () => {
    const dialog = await openAddForm();
    const save = within(dialog).getByRole('button', { name: SAVE });

    expect(save.closest('.overflow-y-auto')).toBeNull();
    expect(within(dialog).getByLabelText('Amount (PLN)')).toHaveFocus();
    expect(within(dialog).getByLabelText('Amount (PLN)')).toHaveAttribute('inputmode', 'decimal');
  });

  it('validates amount and participants before sending', async () => {
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/expenses`, () => {
        create();
        return HttpResponse.json({});
      }),
    );
    const dialog = await openAddForm();
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '1.005');
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await within(dialog).findByText('Enter a positive amount with at most two decimals.');
    expect(create).not.toHaveBeenCalled();

    await userEvent.clear(within(dialog).getByLabelText('Amount (PLN)'));
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '5');
    await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Alex' }));
    await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Bea' }));
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await within(dialog).findByText('Choose at least one person.');
  });

  it('hides funding, payer and split for a personal envelope and names the owner as payer', async () => {
    const dialog = await openAddForm();
    await userEvent.selectOptions(within(dialog).getByLabelText('Envelope'), 'a2');
    expect(within(dialog).queryByLabelText('Funded by')).not.toBeInTheDocument();
    expect(within(dialog).queryByLabelText('Paid by')).not.toBeInTheDocument();
    expect(within(dialog).getByText('Paid by You, the owner of this envelope.')).toBeVisible();
  });

  it('explains household funds: nobody credited, nothing split', async () => {
    const dialog = await openAddForm();
    await userEvent.click(within(dialog).getByRole('radio', { name: 'Household account' }));
    expect(within(dialog).getByText(/nobody is credited and nothing is split/)).toBeVisible();
    expect(within(dialog).queryByRole('checkbox', { name: 'Alex' })).not.toBeInTheDocument();
  });

  it('flags a future date without blocking it', async () => {
    const dialog = await openAddForm();
    await userEvent.clear(within(dialog).getByLabelText('Purchase date'));
    await userEvent.type(within(dialog).getByLabelText('Purchase date'), '2999-01-01');
    expect(await within(dialog).findByText('This date is in the future.')).toBeVisible();
  });
});

describe('Add expense before envelopes arrive', () => {
  it('waits for the envelope list so an envelope is always chosen', async () => {
    server.use(
      http.get(`${BASE}/api/budget/accounts`, async () => {
        await delay(150);
        return HttpResponse.json({ items: [shared, mine], totalCount: 2, page: 1, pageSize: 100 });
      }),
    );
    client.clear();
    renderAt('/budget/expenses');
    await userEvent.click(await screen.findByRole('button', { name: 'Add expense' }));
    // The loading dialog is replaced by the form once envelopes arrive, so look the dialog up again.
    const envelope = await waitFor(() =>
      within(screen.getByRole('dialog')).getByLabelText('Envelope'),
    );
    expect(envelope).toHaveValue('a1');
  });
});

describe('Duplicate hint', () => {
  async function fillAmount(dialog: HTMLElement) {
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '100');
  }

  it('looks up the authorised list with an exact amount, category and ±2 day window', async () => {
    const dialog = await openAddForm();
    await userEvent.clear(within(dialog).getByLabelText('Purchase date'));
    await userEvent.type(within(dialog).getByLabelText('Purchase date'), '2026-10-01');
    await fillAmount(dialog);
    await waitFor(() => {
      expect(listParams.some((p) => p.get('amount') === '100')).toBe(true);
    });
    const hint = listParams.find((p) => p.get('amount') === '100');
    expect(hint?.get('category')).toBe('Groceries');
    expect(hint?.get('from')).toBe('2026-09-29');
    expect(hint?.get('to')).toBe('2026-10-03');
    expect(hint?.get('pageSize')).toBe('5');
    expect(hint?.get('excludeId')).toBeNull();
  });

  it('warns about matches, blocks saving until "Keep both", and can cancel before creating', async () => {
    listItems = [expense];
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/expenses`, async ({ request }) => {
        create(await request.json());
        return HttpResponse.json({ expenseId: 'new', revision: 1, created: true }, { status: 201 });
      }),
    );
    const dialog = await openAddForm();
    await fillAmount(dialog);
    await within(dialog).findByText('1 similar expense already exists');
    expect(within(dialog).getByRole('button', { name: SAVE })).toBeDisabled();

    await userEvent.click(within(dialog).getByRole('button', { name: 'Keep both' }));
    expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await waitFor(() => {
      expect(create).toHaveBeenCalledTimes(1);
    });
  });

  it('cancelling the warning closes the form without creating anything', async () => {
    listItems = [expense];
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/expenses`, () => {
        create();
        return HttpResponse.json({});
      }),
    );
    const dialog = await openAddForm();
    await fillAmount(dialog);
    await within(dialog).findByText('1 similar expense already exists');
    const warning = within(dialog).getByRole('status');
    await userEvent.click(within(warning).getByRole('button', { name: 'Cancel' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(create).not.toHaveBeenCalled();
  });

  it('makes a changed amount re-check and drops the earlier decision', async () => {
    listItems = [expense];
    const dialog = await openAddForm();
    await fillAmount(dialog);
    await within(dialog).findByText('1 similar expense already exists');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Keep both' }));
    expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();

    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '5');
    await waitFor(() => {
      expect(within(dialog).getByRole('button', { name: SAVE })).toBeDisabled();
    });
  });

  it('never calls a failed lookup "no matches": offers retry and an explicit save without checking', async () => {
    listStatus = 500;
    const dialog = await openAddForm();
    await fillAmount(dialog);
    await within(dialog).findByText('Could not check for similar expenses.');
    expect(within(dialog).queryByText(/similar expense/)).toBeInTheDocument();
    expect(within(dialog).getByRole('button', { name: SAVE })).toBeDisabled();

    await userEvent.click(within(dialog).getByRole('button', { name: 'Save without checking' }));
    expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();
  });
});

describe('Submission failures and retries', () => {
  it('keeps the input and reuses the request ID on a retry of the same data', async () => {
    const ids: string[] = [];
    let attempts = 0;
    server.use(
      http.post(`${BASE}/api/budget/expenses`, async ({ request }) => {
        ids.push(((await request.json()) as { clientRequestId: string }).clientRequestId);
        attempts += 1;
        return attempts === 1
          ? new HttpResponse(null, { status: 500 })
          : HttpResponse.json({ expenseId: 'new', revision: 1, created: true }, { status: 201 });
      }),
    );
    const dialog = await openAddForm();
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '20');
    await waitFor(() => {
      expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();
    });
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await within(dialog).findByText('Could not save. Your input is kept — try again.');
    expect(within(dialog).getByLabelText('Amount (PLN)')).toHaveValue('20');

    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await waitFor(() => {
      expect(ids).toHaveLength(2);
    });
    expect(ids[1]).toBe(ids[0]);
  });

  it('starts a new request ID when the data changed after a failure', async () => {
    const ids: string[] = [];
    server.use(
      http.post(`${BASE}/api/budget/expenses`, async ({ request }) => {
        ids.push(((await request.json()) as { clientRequestId: string }).clientRequestId);
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const dialog = await openAddForm();
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '20');
    await waitFor(() => {
      expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();
    });
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await within(dialog).findByText(/Your input is kept/);
    await userEvent.type(within(dialog).getByLabelText('Amount (PLN)'), '5');
    await waitFor(() => {
      expect(within(dialog).getByRole('button', { name: SAVE })).toBeEnabled();
    });
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await waitFor(() => {
      expect(ids).toHaveLength(2);
    });
    expect(ids[1]).not.toBe(ids[0]);
  });
});

describe('Expense list', () => {
  it('shows who recorded and who paid as separate facts, and voided rows', async () => {
    listItems = [
      expense,
      { ...expense, id: 'e2', isVoided: true, paidByPersonId: null, paidByDisplayName: null },
    ];
    renderAt('/budget/expenses');
    await screen.findAllByText(/recorded by Alex/);
    expect(screen.getByText(/paid by Bea/)).toBeInTheDocument();
    expect(screen.getByText(/Paid from household funds/)).toBeInTheDocument();
    expect(screen.getByText('Voided')).toBeInTheDocument();
  });

  it('sends filters to the server and shows a distinct filtered-empty state', async () => {
    renderAt('/budget/expenses');
    await screen.findByText('No expenses yet');
    await userEvent.selectOptions(screen.getByLabelText('Category'), 'Health');
    await screen.findByText('No matching expenses');
    expect(listParams.at(-1)?.get('category')).toBe('Health');
    await userEvent.click(screen.getByRole('checkbox', { name: 'Show voided expenses' }));
    await waitFor(() => {
      expect(listParams.at(-1)?.get('includeVoided')).toBe('true');
    });
  });

  it('offers retry when loading fails', async () => {
    listStatus = 500;
    renderAt('/budget/expenses');
    await screen.findByText(/Could not load Budget/);
    expect(screen.queryByText('No expenses yet')).not.toBeInTheDocument();
  });
});

describe('Expense detail', () => {
  it('shows attribution, stored shares and the history with reasons', async () => {
    renderAt('/budget/expenses/e1');
    await screen.findByRole('heading', { name: /100\.00 PLN · Groceries/ });
    expect(screen.getByText('Recorded by')).toBeInTheDocument();
    const history = screen.getByRole('region', { name: 'History' });
    expect(within(history).getByText(/Revision 1 · Created/)).toBeInTheDocument();
    expect(within(history).getByText(/by Alex/)).toBeInTheDocument();
  });

  it('is a 404 state for an invisible expense', async () => {
    server.use(
      http.get(`${BASE}/api/budget/expenses/e1`, () => new HttpResponse(null, { status: 404 })),
    );
    renderAt('/budget/expenses/e1');
    await screen.findByText("This expense doesn't exist or you can't see it.");
  });

  it('corrects with a reason and the expected revision, excluding itself from duplicate hints', async () => {
    const update = vi.fn();
    server.use(
      http.put(`${BASE}/api/budget/expenses/e1`, async ({ request }) => {
        update(await request.json());
        return HttpResponse.json({ expenseId: 'e1', revision: 2, created: false });
      }),
    );
    renderAt('/budget/expenses/e1');
    await userEvent.click(await screen.findByRole('button', { name: 'Correct' }));
    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByLabelText('Envelope')).toBeDisabled();
    const amount = within(dialog).getByLabelText('Amount (PLN)');
    await userEvent.clear(amount);
    await userEvent.type(amount, '60');
    await waitFor(() => {
      expect(listParams.some((p) => p.get('excludeId') === 'e1')).toBe(true);
    });
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await within(dialog).findByText('Enter a reason.');
    await userEvent.type(within(dialog).getByLabelText('Reason'), 'typo');
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await waitFor(() => {
      expect(update).toHaveBeenCalledTimes(1);
    });
    expect(update.mock.calls[0]?.[0]).toMatchObject({
      expectedRevision: 1,
      reason: 'typo',
      amount: '60',
      paidByPersonId: 'bea',
      participantIds: ['me', 'bea'],
    });
  });

  it('offers reload on a correction conflict and keeps the typed input until then', async () => {
    server.use(
      http.put(`${BASE}/api/budget/expenses/e1`, () => new HttpResponse(null, { status: 409 })),
    );
    renderAt('/budget/expenses/e1');
    await userEvent.click(await screen.findByRole('button', { name: 'Correct' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText('Reason'), 'typo');
    await userEvent.click(within(dialog).getByRole('button', { name: SAVE }));
    await within(dialog).findByText(/Someone else changed this expense/);
    expect(within(dialog).getByLabelText('Reason')).toHaveValue('typo');

    await userEvent.click(within(dialog).getByRole('button', { name: 'Reload' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('voids with a required reason', async () => {
    const voided = vi.fn();
    server.use(
      http.post(`${BASE}/api/budget/expenses/e1/void`, async ({ request }) => {
        voided(await request.json());
        return HttpResponse.json({ expenseId: 'e1', revision: 2, created: false });
      }),
    );
    renderAt('/budget/expenses/e1');
    await userEvent.click(await screen.findByRole('button', { name: 'Void' }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Void expense' }));
    await within(dialog).findByText('Enter a reason.');
    expect(voided).not.toHaveBeenCalled();

    await userEvent.type(within(dialog).getByLabelText('Reason'), 'duplicate');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Void expense' }));
    await waitFor(() => {
      expect(voided).toHaveBeenCalledTimes(1);
    });
    expect(voided.mock.calls[0]?.[0]).toMatchObject({ expectedRevision: 1, reason: 'duplicate' });
  });

  it('offers no actions on a voided expense', async () => {
    server.use(
      http.get(`${BASE}/api/budget/expenses/e1`, () =>
        HttpResponse.json({ ...expense, isVoided: true }),
      ),
    );
    renderAt('/budget/expenses/e1');
    await screen.findByRole('heading', { name: /Groceries/ });
    expect(screen.queryByRole('button', { name: 'Correct' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Void' })).not.toBeInTheDocument();
  });
});
