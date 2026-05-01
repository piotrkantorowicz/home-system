import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import Inbox from './Inbox';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';

function renderInbox() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <MemoryRouter>
        <Inbox />
      </MemoryRouter>
    </Wrapper>,
  );
}

describe('Inbox', () => {
  it('shows empty state when there are no notifications', async () => {
    server.use(
      http.get(`${BASE}/api/notifications`, () =>
        HttpResponse.json({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }),
      ),
    );

    renderInbox();

    await waitFor(() => {
      expect(screen.getByText('inbox.empty_title')).toBeInTheDocument();
    });
    expect(screen.getByText('inbox.empty_body')).toBeInTheDocument();
  });

  it('renders notification items and triggers mark-read on click', async () => {
    const markReadSpy = vi.fn();

    server.use(
      http.get(`${BASE}/api/notifications`, () =>
        HttpResponse.json({
          items: [
            {
              id: '11111111-1111-1111-1111-111111111111',
              type: 'MealReminder',
              title: 'Time for lunch',
              body: 'Lunch is starting',
              createdAt: '2026-05-01T11:00:00Z',
              readAt: null,
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        }),
      ),
      http.post(`${BASE}/api/notifications/:id/read`, ({ params }) => {
        markReadSpy(params.id);
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderInbox();

    await waitFor(() => {
      expect(screen.getByText('Time for lunch')).toBeInTheDocument();
    });

    expect(screen.getByText('Lunch is starting')).toBeInTheDocument();

    const button = screen.getByRole('button', { name: 'inbox.mark_read_aria' });
    await userEvent.click(button);

    await waitFor(() => {
      expect(markReadSpy).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111');
    });
  });

  it('shows error banner with retry button on fetch failure', async () => {
    server.use(
      http.get(`${BASE}/api/notifications`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    renderInbox();

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    expect(screen.getByText('inbox.error')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'inbox.retry' })).toBeInTheDocument();
  });
});
