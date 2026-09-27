import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { adminModule } from '../index';

import DeadLetters from './DeadLetters';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

const auth = vi.hoisted(() => ({ roles: [] as string[] }));

vi.mock('react-oidc-context', () => ({
  useAuth: () => ({ isAuthenticated: true, user: { profile: { sub: 'me', roles: auth.roles } } }),
}));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: vi.fn(),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050/api/admin';

function renderPage() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>
        <DeadLetters />
      </ToastProvider>
    </Wrapper>,
  );
}

beforeEach(() => {
  initI18n([adminModule]);
  auth.roles = ['admin'];
  server.use(
    http.get(`${BASE}/notifications/deliveries/summary`, () =>
      HttpResponse.json({ deadLettered: 1, retrying: 2 }),
    ),
    http.get(`${BASE}/notifications/deliveries/dead-letters`, () =>
      HttpResponse.json({
        items: [
          {
            deliveryId: 'd-1',
            notificationId: 'n-1',
            userId: 'user-7',
            type: 'WaterReminder',
            title: 'Drink water',
            channel: 'Email',
            attemptCount: 5,
            lastAttemptAt: '2026-09-12T10:00:00Z',
            failureReason: 'smtp down',
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 25,
      }),
    ),
    http.get(`${BASE}/outbox/summary`, () =>
      HttpResponse.json([
        { module: 'Household', deadLettered: 1, retrying: 0 },
        { module: 'DietPlanner', deadLettered: 0, retrying: 3 },
      ]),
    ),
    http.get(`${BASE}/outbox/Household/dead-letters`, () =>
      HttpResponse.json({
        items: [
          {
            id: 'm-1',
            eventId: 'e-1',
            eventType: 'Household.Contracts.Events.MemberJoined, Household.Contracts',
            occurredAt: '2026-09-12T09:00:00Z',
            attemptCount: 10,
            lastError: 'handler threw',
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 25,
      }),
    ),
  );
});

describe('DeadLetters', () => {
  it('tells a non-admin the page is for admins only', () => {
    auth.roles = [];
    renderPage();

    expect(screen.getByText('Admins only')).toBeInTheDocument();
  });

  it('shows the backlog counters', async () => {
    renderPage();

    const tile = (label: string) => screen.getByText(label).parentElement ?? document.body;

    expect(await within(tile('Retrying events')).findByText('3')).toBeInTheDocument();
    expect(await within(tile('Dead deliveries')).findByText('1')).toBeInTheDocument();
  });

  it('lists dead-lettered deliveries and events of modules that have any', async () => {
    renderPage();

    const deliveries = await screen.findByRole('table', { name: 'Notification deliveries' });
    expect(await within(deliveries).findByText('Drink water')).toBeInTheDocument();
    expect(within(deliveries).getByText('smtp down')).toBeInTheDocument();

    const events = await screen.findByRole('table', { name: 'Household dead-lettered events' });
    expect(await within(events).findByText('MemberJoined')).toBeInTheDocument();
    expect(screen.queryByRole('table', { name: 'DietPlanner dead-lettered events' })).toBeNull();
  });

  it('retries a delivery and confirms it was queued', async () => {
    const retried = vi.fn();
    server.use(
      http.post(`${BASE}/notifications/deliveries/:id/retry`, ({ params }) => {
        retried(params.id);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();

    const deliveries = await screen.findByRole('table', { name: 'Notification deliveries' });
    await userEvent.click(await within(deliveries).findByRole('button', { name: 'Retry' }));

    expect(await screen.findByText('Queued for retry')).toBeInTheDocument();
    expect(retried).toHaveBeenCalledExactlyOnceWith('d-1');
  });

  it('retries an event through its module route', async () => {
    const retried = vi.fn();
    server.use(
      http.post(`${BASE}/outbox/:module/dead-letters/:id/retry`, ({ params }) => {
        retried(params.module, params.id);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();

    const events = await screen.findByRole('table', { name: 'Household dead-lettered events' });
    await userEvent.click(await within(events).findByRole('button', { name: 'Retry' }));

    expect(await screen.findByText('Queued for retry')).toBeInTheDocument();
    expect(retried).toHaveBeenCalledExactlyOnceWith('Household', 'm-1');
  });

  it('steps back a page when retrying empties the last delivery page', async () => {
    const ids = Array.from({ length: 26 }, (_, i) => `d-${String(i + 1)}`);
    server.use(
      http.get(`${BASE}/notifications/deliveries/dead-letters`, ({ request }) => {
        const url = new URL(request.url);
        const page = Number(url.searchParams.get('page'));
        const pageSize = Number(url.searchParams.get('pageSize'));
        return HttpResponse.json({
          items: ids.slice((page - 1) * pageSize, page * pageSize).map((id) => ({
            deliveryId: id,
            notificationId: 'n-1',
            userId: 'user-7',
            type: 'WaterReminder',
            title: `Title ${id}`,
            channel: 'Email',
            attemptCount: 5,
            lastAttemptAt: '2026-09-12T10:00:00Z',
            failureReason: 'smtp down',
          })),
          totalCount: ids.length,
          page,
          pageSize,
        });
      }),
      http.post(`${BASE}/notifications/deliveries/:id/retry`, ({ params }) => {
        ids.splice(ids.indexOf(String(params.id)), 1);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();

    const deliveries = await screen.findByRole('table', { name: 'Notification deliveries' });
    await userEvent.click(await screen.findByRole('button', { name: 'Next' }));
    await within(deliveries).findByText('Title d-26');
    await userEvent.click(within(deliveries).getByRole('button', { name: 'Retry' }));

    expect(await within(deliveries).findByText('Title d-1')).toBeInTheDocument();
    expect(screen.queryByText('No dead-lettered deliveries')).toBeNull();
  });

  it('steps back a page when retrying empties the last event page', async () => {
    const ids = Array.from({ length: 26 }, (_, i) => `m-${String(i + 1)}`);
    server.use(
      http.get(`${BASE}/outbox/Household/dead-letters`, ({ request }) => {
        const url = new URL(request.url);
        const page = Number(url.searchParams.get('page'));
        const pageSize = Number(url.searchParams.get('pageSize'));
        return HttpResponse.json({
          items: ids.slice((page - 1) * pageSize, page * pageSize).map((id) => ({
            id,
            eventId: `e-${id}`,
            eventType: `Household.Contracts.Events.Event${id}, Household.Contracts`,
            occurredAt: '2026-09-12T09:00:00Z',
            attemptCount: 10,
            lastError: 'handler threw',
          })),
          totalCount: ids.length,
          page,
          pageSize,
        });
      }),
      http.post(`${BASE}/outbox/:module/dead-letters/:id/retry`, ({ params }) => {
        ids.splice(ids.indexOf(String(params.id)), 1);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();

    const events = await screen.findByRole('table', { name: 'Household dead-lettered events' });
    await userEvent.click(await screen.findByRole('button', { name: 'Next' }));
    await within(events).findByText('Eventm-26');
    await userEvent.click(within(events).getByRole('button', { name: 'Retry' }));

    expect(await within(events).findByText('Eventm-1')).toBeInTheDocument();
  });
});
