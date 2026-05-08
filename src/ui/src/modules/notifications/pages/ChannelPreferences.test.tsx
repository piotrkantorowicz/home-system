import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import ChannelPreferences from './ChannelPreferences';

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

const defaultPreferences = {
  consoleEnabled: true,
  emailEnabled: false,
  webSocketEnabled: false,
};

function renderPage() {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <MemoryRouter>
        <ChannelPreferences />
      </MemoryRouter>
    </Wrapper>,
  );
}

describe('ChannelPreferences', () => {
  it('renders the email and websocket toggle rows after loading (console toggle hidden)', async () => {
    server.use(
      http.get(`${BASE}/api/notification-preferences`, () => HttpResponse.json(defaultPreferences)),
    );

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByRole('switch', { name: 'preferences.channel.websocket' }),
      ).toBeInTheDocument();
    });

    expect(screen.getByRole('switch', { name: 'preferences.channel.email' })).toBeInTheDocument();
    expect(
      screen.queryByRole('switch', { name: 'preferences.channel.console' }),
    ).not.toBeInTheDocument();
  });

  it('calls PUT when the websocket toggle is changed', async () => {
    const putSpy = vi.fn();

    server.use(
      http.get(`${BASE}/api/notification-preferences`, () => HttpResponse.json(defaultPreferences)),
      http.put(`${BASE}/api/notification-preferences`, async ({ request }) => {
        putSpy(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByRole('switch', { name: 'preferences.channel.websocket' }),
      ).toBeInTheDocument();
    });

    const websocketToggle = screen.getByRole('switch', { name: 'preferences.channel.websocket' });
    await userEvent.click(websocketToggle);

    await waitFor(
      () => {
        expect(putSpy).toHaveBeenCalledWith({ ...defaultPreferences, webSocketEnabled: true });
      },
      { timeout: 1000 },
    );
  });

  it('shows error banner with retry button on fetch failure', async () => {
    server.use(
      http.get(`${BASE}/api/notification-preferences`, () =>
        HttpResponse.json({ title: 'Internal Server Error' }, { status: 500 }),
      ),
    );

    renderPage();

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    expect(screen.getByText('preferences.error')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'preferences.retry' })).toBeInTheDocument();
  });

  it('shows save-failed message when PUT request fails', async () => {
    server.use(
      http.get(`${BASE}/api/notification-preferences`, () => HttpResponse.json(defaultPreferences)),
      http.put(`${BASE}/api/notification-preferences`, () =>
        HttpResponse.json({ title: 'Error' }, { status: 500 }),
      ),
    );

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByRole('switch', { name: 'preferences.channel.websocket' }),
      ).toBeInTheDocument();
    });

    const websocketToggle = screen.getByRole('switch', { name: 'preferences.channel.websocket' });
    await userEvent.click(websocketToggle);

    await waitFor(
      () => {
        expect(screen.getByText('preferences.save_failed')).toBeInTheDocument();
      },
      { timeout: 1000 },
    );
  });
});
