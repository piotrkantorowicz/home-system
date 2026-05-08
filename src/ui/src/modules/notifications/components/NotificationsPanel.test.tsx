import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { NotificationsPanel } from './NotificationsPanel';

import type { NotificationDto } from '../api/hooks/useNotifications';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

interface NotificationsResult {
  data: { items: NotificationDto[]; totalPages: number } | undefined;
  isLoading: boolean;
  isError: boolean;
  refetch: () => void;
}

interface UnreadCountResult {
  data: { total: number } | undefined;
}

const mutateMock = vi.fn();
const useNotificationsMock = vi.fn<() => NotificationsResult>();
const useUnreadCountMock = vi.fn<() => UnreadCountResult>();

vi.mock('../api/hooks/useMarkRead', () => ({
  useMarkRead: () => ({ mutate: mutateMock }),
}));

vi.mock('../api/hooks/useNotifications', () => ({
  useNotifications: (): NotificationsResult => useNotificationsMock(),
}));

vi.mock('../api/hooks/useUnreadCount', () => ({
  useUnreadCount: (): UnreadCountResult => useUnreadCountMock(),
}));

const sampleItem: NotificationDto = {
  id: '11111111-1111-1111-1111-111111111111',
  type: 'MealReminder',
  title: 'Time for lunch',
  body: 'Lunch is starting',
  createdAt: '2026-05-01T11:00:00Z',
  readAt: null,
};

function renderPanel() {
  return render(
    <MemoryRouter>
      <NotificationsPanel />
    </MemoryRouter>,
  );
}

describe('NotificationsPanel', () => {
  beforeEach(() => {
    mutateMock.mockReset();
    useUnreadCountMock.mockReturnValue({ data: { total: 0 } });
    useNotificationsMock.mockReturnValue({
      data: { items: [sampleItem], totalPages: 1 },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });
  });

  it('opens the panel when the bell trigger is clicked', async () => {
    renderPanel();

    expect(screen.queryByText('panel.title')).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'panel.open_aria' }));
    expect(screen.getByText('panel.title')).toBeInTheDocument();
  });

  it('shows recent notifications inside the panel', async () => {
    renderPanel();
    await userEvent.click(screen.getByRole('button', { name: 'panel.open_aria' }));
    expect(screen.getByText('Time for lunch')).toBeInTheDocument();
  });

  it('renders the empty state when there are no items', async () => {
    useNotificationsMock.mockReturnValue({
      data: { items: [], totalPages: 0 },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });
    renderPanel();
    await userEvent.click(screen.getByRole('button', { name: 'panel.open_aria' }));
    expect(screen.getByText('inbox.empty_title')).toBeInTheDocument();
  });

  it('renders footer links to inbox and preferences', async () => {
    renderPanel();
    await userEvent.click(screen.getByRole('button', { name: 'panel.open_aria' }));

    expect(screen.getByRole('link', { name: 'panel.view_all' })).toHaveAttribute(
      'href',
      '/notifications',
    );
    expect(screen.getByRole('link', { name: /panel.preferences_link/ })).toHaveAttribute(
      'href',
      '/notifications/preferences',
    );
  });

  it('marks an unread notification as read when its row is activated', async () => {
    renderPanel();
    await userEvent.click(screen.getByRole('button', { name: 'panel.open_aria' }));
    await userEvent.click(screen.getByRole('button', { name: 'inbox.mark_read_aria' }));
    expect(mutateMock).toHaveBeenCalledWith(sampleItem.id);
  });
});
