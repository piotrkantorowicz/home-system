import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import Inbox from './Inbox';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { count?: number }) =>
      opts?.count !== undefined ? `${key}:${String(opts.count)}` : key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

const iso = (hoursAgo: number) => new Date(Date.now() - hoursAgo * 3_600_000).toISOString();

function item(n: number, readAt: string | null, hoursAgo = 0.01) {
  return {
    id: `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`,
    type: 'MealReminder',
    title: `Notification ${String(n)}`,
    body: `Body ${String(n)}`,
    createdAt: iso(hoursAgo),
    readAt,
  };
}

function paged(items: ReturnType<typeof item>[], page = 1, pageSize = 20) {
  const slice = items.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(items.length / pageSize);
  return {
    items: slice,
    page,
    pageSize,
    totalCount: items.length,
    totalPages,
    hasNextPage: page < totalPages,
    hasPreviousPage: page > 1,
  };
}

function serve(all: ReturnType<typeof item>[]) {
  server.use(
    http.get(`${BASE}/api/notifications/unread-count`, () =>
      HttpResponse.json({ total: all.filter((n) => !n.readAt).length }),
    ),
    http.get(`${BASE}/api/notifications`, ({ request }) => {
      const url = new URL(request.url);
      return HttpResponse.json(
        paged(
          all,
          Number(url.searchParams.get('page') ?? 1),
          Number(url.searchParams.get('pageSize') ?? 20),
        ),
      );
    }),
  );
}

function renderInbox(client?: QueryClient) {
  const Wrapper = createWrapper(client);
  return render(
    <Wrapper>
      <MemoryRouter>
        <Inbox />
      </MemoryRouter>
    </Wrapper>,
  );
}

describe('Inbox', () => {
  it('shows the all-empty state when there are no notifications', async () => {
    serve([]);
    renderInbox();
    expect(await screen.findByText('inbox.empty_title')).toBeInTheDocument();
    expect(screen.getByText('inbox.empty_body')).toBeInTheDocument();
  });

  it('groups items into Today and Earlier, with no selection checkboxes', async () => {
    serve([item(1, null, 0.01), item(2, iso(40), 48)]);
    renderInbox();
    const today = await screen.findByRole('region', { name: 'inbox.group_today' });
    const earlier = screen.getByRole('region', { name: 'inbox.group_earlier' });
    expect(within(today).getByText('Notification 1')).toBeInTheDocument();
    expect(within(earlier).getByText('Notification 2')).toBeInTheDocument();
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
  });

  it('marks an item read and keeps the unread total real', async () => {
    const readSpy = vi.fn();
    serve([item(1, null)]);
    server.use(
      http.post(`${BASE}/api/notifications/:id/read`, ({ params }) => {
        readSpy(params.id);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderInbox();
    expect(await screen.findByText('inbox.subtitle_unread:1')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: /Notification 1/ }));
    await waitFor(() => {
      expect(readSpy).toHaveBeenCalledWith('00000000-0000-0000-0000-000000000001');
    });
  });

  it('Unread chip shows the real total and pages across server pages', async () => {
    // 130 notifications: 25 unread, spread so the unread rows span server pages (pageSize 100).
    const all = Array.from({ length: 130 }, (_, i) =>
      item(i + 1, i % 5 === 0 ? null : iso(1), 0.01 + i / 100),
    );
    const unreadCount = all.filter((n) => !n.readAt).length; // 26
    serve(all);
    renderInbox();

    const unreadChip = await screen.findByRole('radio', {
      name: `inbox.filter_unread:${String(unreadCount)}`,
    });
    await userEvent.click(unreadChip);

    expect(await screen.findByText('Notification 1')).toBeInTheDocument();
    expect(screen.queryByText('Notification 2')).not.toBeInTheDocument();
    expect(screen.getByText('inbox.page_indicator')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'inbox.next_page' }));
    // page 2 of unread holds the remaining 6 unread rows (21..26th unread = Notification 101..126)
    expect(await screen.findByText('Notification 101')).toBeInTheDocument();
    expect(screen.queryByText('Notification 1')).not.toBeInTheDocument();
  });

  it('Unread filter shows the caught-up empty state when nothing is unread', async () => {
    serve([item(1, iso(1))]);
    renderInbox();
    await userEvent.click(await screen.findByRole('radio', { name: 'inbox.filter_unread:0' }));
    await waitFor(() => {
      expect(screen.getAllByText('inbox.caught_up').length).toBeGreaterThan(0);
    });
  });

  it('mark all read has no row cap: 2,100 unread rows across 21 server pages', async () => {
    const all = Array.from({ length: 2100 }, (_, i) => item(i + 1, null, 0.01 + i / 1000));
    const bulk = vi.fn();
    serve(all);
    server.use(
      http.post(`${BASE}/api/notifications/read`, async ({ request }) => {
        bulk(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderInbox();
    await userEvent.click(await screen.findByRole('button', { name: 'inbox.mark_all_read' }));
    await waitFor(() => {
      expect(bulk).toHaveBeenCalledTimes(1);
    });
    expect((bulk.mock.calls[0]?.[0] as { ids: string[] }).ids).toHaveLength(2100);
  });

  it('mark all read covers the whole list, not just the visible page', async () => {
    const all = Array.from({ length: 45 }, (_, i) => item(i + 1, null, 0.01 + i / 100));
    const bulk = vi.fn();
    serve(all);
    server.use(
      http.post(`${BASE}/api/notifications/read`, async ({ request }) => {
        bulk(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderInbox();
    await userEvent.click(await screen.findByRole('button', { name: 'inbox.mark_all_read' }));
    await waitFor(() => {
      expect(bulk).toHaveBeenCalledTimes(1);
    });
    const body = bulk.mock.calls[0]?.[0] as { ids: string[] };
    expect(body.ids).toHaveLength(45);
  });

  it('shows a new notification after the stream invalidates the cache', async () => {
    const all = [item(1, null)];
    serve(all);
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    renderInbox(client);
    await screen.findByText('Notification 1');

    all.unshift(item(2, null));
    // What useNotificationStream does when a hub message arrives.
    await client.invalidateQueries({ queryKey: ['notifications'] });
    expect(await screen.findByText('Notification 2')).toBeInTheDocument();
  });

  it('links to App & account settings', async () => {
    serve([]);
    renderInbox();
    expect(await screen.findByRole('link', { name: 'inbox.settings_aria' })).toHaveAttribute(
      'href',
      '/diet-planner/settings/app',
    );
  });

  it('shows error banner with retry button on fetch failure', async () => {
    server.use(
      http.get(`${BASE}/api/notifications/unread-count`, () => HttpResponse.json({ total: 0 })),
      http.get(`${BASE}/api/notifications`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );
    renderInbox();
    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('inbox.error')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'inbox.retry' })).toBeInTheDocument();
  });
});
