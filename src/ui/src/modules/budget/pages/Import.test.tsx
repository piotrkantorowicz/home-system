import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetLayout } from '../components/BudgetLayout';
import { budgetModule } from '../index';

import ExpensesPage from './ExpensesPage';
import ImportPage from './ImportPage';

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
const KEY = (c: string) => c.repeat(64);
const account = (id: string, name: string, visibility: string, owner: string | null = null) => ({
  id,
  name,
  visibility,
  ownerPersonId: owner,
  isArchived: false,
  revision: 1,
  createdAt: '2026-10-02T10:00:00Z',
});
const candidate = (key: string, description: string, status: string, extra = {}) => ({
  lineNumber: 1,
  sourceKey: KEY(key),
  occurredOn: '2026-10-18',
  amount: '12.50',
  description,
  merchantKey: description.toUpperCase(),
  suggestedCategory: null,
  status,
  matches: [],
  ...extra,
});
const match = {
  id: 'e1',
  accountId: 'a1',
  amount: '12.50',
  category: 'Groceries',
  description: 'Bakery run',
  occurredOn: '2026-10-17',
  fundingSource: 'Individual',
  paidByPersonId: 'me',
  paidByDisplayName: 'Alex',
  addedByPersonId: 'me',
  addedByDisplayName: 'Alex',
  revision: 1,
  createdAt: '2026-10-17T10:00:00Z',
  isVoided: false,
  shares: [],
};
const preview = {
  profile: 'mbank',
  periodFrom: '2026-10-01',
  periodTo: '2026-10-31',
  accountId: 'a1',
  candidates: [
    candidate('a', 'Pharmacy', 'New', { suggestedCategory: 'Health' }),
    candidate('b', 'Bakery', 'PossibleDuplicate', { matches: [match] }),
    candidate('c', 'Fuel', 'AlreadyImported'),
  ],
  skipped: [
    {
      lineNumber: 9,
      occurredOn: '2026-10-05',
      amount: '3000.00',
      currency: 'PLN',
      description: 'Salary',
      reason: 'Incoming',
    },
  ],
  issues: [],
};

let client: QueryClient;
let previewForms: string[];
let importBodies: { clientRequestId: string; accountId: string; rows: Record<string, unknown>[] }[];
let importResponse: () => Response;

function renderAt(path: string) {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <HouseholdStateContext value={{ ...household }}>
        <ToastProvider>
          <MemoryRouter initialEntries={[path]}>
            <Routes>
              <Route path="/budget" element={<BudgetLayout />}>
                <Route path="expenses" element={<ExpensesPage />} />
                <Route path="import" element={<ImportPage />} />
              </Route>
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </HouseholdStateContext>
    </Wrapper>,
  );
}

beforeEach(() => {
  initI18n([budgetModule]);
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  household.myRole = 'Owner';
  previewForms = [];
  importBodies = [];
  importResponse = () =>
    HttpResponse.json({ expenseIds: ['x1', 'x2'], created: true }, { status: 201 });
  server.use(
    http.get(`${BASE}/api/budget`, () => HttpResponse.json({ id: 'b1', currency: 'PLN' })),
    http.get(`${BASE}/api/budget/accounts`, () =>
      HttpResponse.json({
        items: [account('a1', 'Everyday', 'Household'), account('a2', 'Mine', 'Personal', 'me')],
        totalCount: 2,
        page: 1,
        pageSize: 100,
      }),
    ),
    http.get(`${BASE}/api/budget/expenses`, () =>
      HttpResponse.json({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 25,
        activeCount: 0,
        totalAmount: '0.00',
        yourShareAmount: '0.00',
        dailyTotals: [],
      }),
    ),
    http.post(`${BASE}/api/budget/imports/preview`, ({ request }) => {
      // Reading a jsdom File body never resolves under MSW, so only the encoding is checked here.
      previewForms.push(request.headers.get('content-type') ?? '');
      return HttpResponse.json(preview);
    }),
    http.post(`${BASE}/api/budget/imports`, async ({ request }) => {
      importBodies.push((await request.json()) as (typeof importBodies)[number]);
      return importResponse();
    }),
  );
});

async function previewStatement(envelope = 'Everyday') {
  renderAt('/budget/import');
  const envelopeSelect = await screen.findByLabelText('Envelope');
  await screen.findByRole('option', { name: envelope });
  await userEvent.selectOptions(envelopeSelect, envelope);
  await userEvent.upload(
    screen.getByLabelText('Statement file'),
    new File(['csv'], 'statement.csv', { type: 'text/csv' }),
  );
  await userEvent.click(screen.getByRole('button', { name: 'Preview' }));
  return screen.findByRole('region', { name: 'Statement rows' });
}

describe('Import statement', () => {
  it('is reachable from the expense list for adults', async () => {
    renderAt('/budget/expenses');

    expect(await screen.findByRole('link', { name: 'Import statement' })).toHaveAttribute(
      'href',
      '/budget/import',
    );
  });

  it('uploads the file for the chosen envelope and shows new, duplicate and skipped rows', async () => {
    const review = await previewStatement();

    expect(previewForms[0]).toMatch(/^multipart\/form-data; boundary=/);
    expect(within(review).getByRole('checkbox', { name: 'Import Pharmacy' })).toBeChecked();
    const duplicate = within(review).getByRole('checkbox', { name: 'Import Bakery' });
    expect(duplicate).not.toBeChecked();
    expect(duplicate).toBeEnabled();
    expect(within(review).getByText(/Looks like Bakery run/)).toBeInTheDocument();
    expect(within(review).getByRole('checkbox', { name: 'Import Fuel' })).toBeDisabled();
    expect(within(review).getByRole('combobox', { name: 'Category for Pharmacy' })).toHaveValue(
      'Health',
    );
    expect(within(review).getByText('1 row skipped')).toBeInTheDocument();
  });

  it('sends only the selected rows with their edits and shows the result', async () => {
    const review = await previewStatement();
    const [pharmacy, bakery] = within(review).getAllByRole('listitem');
    if (!pharmacy || !bakery) throw new Error('rows missing');

    await userEvent.click(within(bakery).getByRole('checkbox'));
    await userEvent.selectOptions(within(bakery).getByRole('combobox'), 'Groceries');
    const description = within(pharmacy).getByRole('textbox');
    await userEvent.clear(description);
    await userEvent.type(description, 'Vitamins');
    await userEvent.click(screen.getByRole('button', { name: 'Import 2 expenses' }));

    expect(await screen.findByText('2 expenses imported')).toBeInTheDocument();
    expect(importBodies[0]?.accountId).toBe('a1');
    expect(importBodies[0]?.rows).toEqual([
      expect.objectContaining({
        sourceKey: KEY('a'),
        description: 'Vitamins',
        category: 'Health',
        merchantKey: 'PHARMACY',
        fundingSource: 'Individual',
        paidByPersonId: 'me',
        participantIds: ['me', 'bea'],
      }),
      expect.objectContaining({
        sourceKey: KEY('b'),
        category: 'Groceries',
        merchantKey: 'BAKERY',
      }),
    ]);
    expect(screen.getByRole('link', { name: 'View expenses' })).toHaveAttribute(
      'href',
      '/budget/expenses',
    );
  });

  it('does not teach a merchant an untouched default category', async () => {
    await previewStatement();
    await userEvent.click(screen.getByRole('checkbox', { name: 'Import Bakery' }));
    await userEvent.click(screen.getByRole('button', { name: 'Import 2 expenses' }));

    await screen.findByText('2 expenses imported');
    expect(importBodies[0]?.rows[1]).not.toHaveProperty('merchantKey');
  });

  it('imports into a personal envelope without funding, payer or split', async () => {
    await previewStatement('Mine');

    expect(screen.queryByLabelText('Paid from')).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Import 1 expense' }));

    await screen.findByText('2 expenses imported');
    expect(importBodies[0]?.rows[0]).toMatchObject({
      fundingSource: null,
      paidByPersonId: null,
      participantIds: null,
    });
  });

  it('shows a rejected row under that row and writes nothing', async () => {
    importResponse = () =>
      HttpResponse.json(
        { status: 422, detail: 'Rows[0]: Participants must be current owners or adults.' },
        { status: 422 },
      );
    const review = await previewStatement();
    await userEvent.click(screen.getByRole('button', { name: 'Import 1 expense' }));

    const pharmacy = within(review).getAllByRole('listitem')[0];
    if (!pharmacy) throw new Error('row missing');
    expect(
      await within(pharmacy).findByText('Participants must be current owners or adults.'),
    ).toBeInTheDocument();
    expect(screen.getByText(/Some rows were rejected/)).toBeInTheDocument();
  });

  it('on a conflict offers a fresh preview that keeps the edits', async () => {
    importResponse = () => HttpResponse.json({ status: 409 }, { status: 409 });
    const review = await previewStatement();
    const description = within(review).getAllByRole('textbox')[0];
    if (!description) throw new Error('row missing');
    await userEvent.clear(description);
    await userEvent.type(description, 'Vitamins');
    await userEvent.click(screen.getByRole('button', { name: 'Import 1 expense' }));

    await screen.findByText(/imported since the preview/);
    // The form's own button and the banner's both re-run the preview; use the banner's.
    const again = screen.getAllByRole('button', { name: 'Preview again' }).at(-1);
    if (!again) throw new Error('no retry');
    await userEvent.click(again);

    expect(previewForms).toHaveLength(2);
    expect(await screen.findByDisplayValue('Vitamins')).toBeInTheDocument();
  });

  it('explains a file the server cannot read', async () => {
    server.use(
      http.post(`${BASE}/api/budget/imports/preview`, () =>
        HttpResponse.json({ status: 422, detail: 'Unrecognised format.' }, { status: 422 }),
      ),
    );
    renderAt('/budget/import');
    await screen.findByRole('option', { name: 'Everyday' });
    await userEvent.upload(
      screen.getByLabelText('Statement file'),
      new File(['x'], 'other.csv', { type: 'text/csv' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Preview' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/not a statement we can read/);
  });
});
