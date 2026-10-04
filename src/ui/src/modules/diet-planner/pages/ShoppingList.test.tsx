import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { HouseholdWrapper } from '../../../test/utils/householdWrapper';
import { createWrapper } from '../../../test/utils/queryWrapper';

import ShoppingList from './ShoppingList';

import { server } from '@/test/mocks/server';

const BASE = 'http://localhost:5050';
const LIST = `${BASE}/api/v1/meals/shopping-list`;

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, o?: Record<string, unknown>) =>
      o && 'done' in o ? `${String(o.done)}/${String(o.total)}` : key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
}));

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const seed = [
  { productId: 'p-1', productName: 'Flour', totalAmount: 1200, unit: 'g', isChecked: false },
  {
    productId: 'p-2',
    productName: 'Milk',
    totalAmount: 1000,
    unit: 'ml',
    isChecked: false,
  },
  { productId: 'p-3', productName: 'Eggs', totalAmount: 8, unit: 'piece', isChecked: true },
];

function renderPage() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <HouseholdWrapper>
        <ToastProvider>
          <ShoppingList />
        </ToastProvider>
      </HouseholdWrapper>
    </Wrapper>,
  );
}

/** The page's default range: this week, Monday to Sunday, as `from:to`. */
function range() {
  const iso = (d: Date) =>
    `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const monday = new Date(today);
  monday.setDate(today.getDate() - today.getDay() + (today.getDay() === 0 ? -6 : 1));
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return `${iso(monday)}:${iso(sunday)}`;
}

describe('ShoppingList', () => {
  // A tiny stateful backend: GET reflects what PUT/DELETE did, like the real household store.
  let items = seed;
  beforeEach(() => {
    window.localStorage.clear();
    items = seed.map((i) => ({ ...i }));
    server.use(
      http.get(LIST, () => HttpResponse.json(items)),
      http.put(`${LIST}/checks`, async ({ request }) => {
        const b = (await request.json()) as { productId: string; isChecked: boolean };
        items = items.map((i) =>
          i.productId === b.productId ? { ...i, isChecked: b.isChecked } : i,
        );
        return new HttpResponse(null, { status: 204 });
      }),
      http.delete(`${LIST}/checks`, () => {
        items = items.map((i) => ({ ...i, isChecked: false }));
        return new HttpResponse(null, { status: 204 });
      }),
    );
  });

  it('shows mixed units, a bought section and progress', async () => {
    renderPage();
    expect(await screen.findByText('Flour')).toBeInTheDocument();
    expect(screen.getByText('1.2 kg')).toBeInTheDocument();
    expect(screen.getByText('1 L')).toBeInTheDocument();
    expect(screen.getByText('8 pcs')).toBeInTheDocument();
    expect(screen.getByTestId('shopping-list-progress')).toHaveTextContent('1/3');
    expect(screen.getAllByRole('checkbox')).toHaveLength(3);
  });

  it('sends the check to the household API and keeps it', async () => {
    let body: unknown;
    server.use(
      http.put(`${LIST}/checks`, async ({ request }) => {
        body = await request.json();
        items = items.map((i) => (i.productId === 'p-1' ? { ...i, isChecked: true } : i));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    const row = (await screen.findByText('Flour')).closest('label') as HTMLElement;
    await userEvent.click(within(row).getByRole('checkbox'));
    await waitFor(() => {
      expect(screen.getByTestId('shopping-list-progress')).toHaveTextContent('2/3');
    });
    expect(body).toMatchObject({ productId: 'p-1', unit: 'g', isChecked: true });
  });

  it('keeps the check on this device, labelled, when the API refuses', async () => {
    server.use(http.put(`${LIST}/checks`, () => new HttpResponse(null, { status: 500 })));
    renderPage();
    const row = (await screen.findByText('Flour')).closest('label') as HTMLElement;
    await userEvent.click(within(row).getByRole('checkbox'));
    expect(await screen.findByText('shopping_list.saved_locally')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId('shopping-list-progress')).toHaveTextContent('2/3');
    });
    const stored = window.localStorage.getItem('home-system-shopping-local:home-1:' + range());
    expect(JSON.parse(stored ?? '{}')).toEqual({ 'p-1|g': true });
  });

  it('clears the local entry once the API accepts the check', async () => {
    const key = 'home-system-shopping-local:home-1:' + range();
    window.localStorage.setItem(key, JSON.stringify({ 'p-1|g': true }));
    renderPage();
    // The kept check is shown, then re-synced to the API and dropped locally.
    await waitFor(() => {
      expect(window.localStorage.getItem(key)).toBeNull();
    });
    expect(items.find((i) => i.productId === 'p-1')?.isChecked).toBe(true);
    await waitFor(() => {
      expect(screen.queryByText('shopping_list.saved_locally')).not.toBeInTheDocument();
    });
  });

  it('keeps entries separate per household and range', async () => {
    window.localStorage.setItem('home-system-shopping-local:other:' + range(), '{"p-1|g":true}');
    renderPage();
    await screen.findByText('Flour');
    expect(screen.queryByText('shopping_list.saved_locally')).not.toBeInTheDocument();
    expect(screen.getByTestId('shopping-list-progress')).toHaveTextContent('1/3');
  });

  it('unchecks everything', async () => {
    renderPage();
    await userEvent.click(await screen.findByTestId('shopping-list-uncheck-all'));
    await waitFor(() => {
      expect(screen.getByTestId('shopping-list-progress')).toHaveTextContent('0/3');
    });
  });

  it('shows the empty state', async () => {
    server.use(http.get(LIST, () => HttpResponse.json([])));
    renderPage();
    expect(await screen.findByText('shopping_list.empty_title')).toBeInTheDocument();
  });

  it('shows an error with retry', async () => {
    server.use(http.get(LIST, () => new HttpResponse(null, { status: 500 })));
    renderPage();
    expect(await screen.findByText('shopping_list.load_failed')).toBeInTheDocument();
  });
});
