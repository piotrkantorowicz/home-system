import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { NotificationListItem } from './NotificationListItem';

import type { NotificationDto } from '../api/hooks/useNotifications';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

const NOW = new Date('2026-05-01T12:00:00Z');

const baseNotification: NotificationDto = {
  id: '11111111-1111-1111-1111-111111111111',
  type: 'MealReminder',
  title: 'Time for lunch',
  body: 'Lunch is starting',
  createdAt: '2026-05-01T11:00:00Z',
  readAt: null,
};

describe('NotificationListItem', () => {
  it('renders the title and body', () => {
    render(<NotificationListItem notification={baseNotification} onActivate={vi.fn()} now={NOW} />);

    expect(screen.getByText('Time for lunch')).toBeInTheDocument();
    expect(screen.getByText('Lunch is starting')).toBeInTheDocument();
  });

  it('marks unread items with aria-pressed=false and a label', () => {
    render(<NotificationListItem notification={baseNotification} onActivate={vi.fn()} now={NOW} />);

    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('aria-pressed', 'false');
    expect(button).toHaveAccessibleName('inbox.mark_read_aria');
  });

  it('marks read items with aria-pressed=true', () => {
    render(
      <NotificationListItem
        notification={{ ...baseNotification, readAt: '2026-05-01T11:30:00Z' }}
        onActivate={vi.fn()}
        now={NOW}
      />,
    );
    expect(screen.getByRole('button')).toHaveAttribute('aria-pressed', 'true');
  });

  it('calls onActivate with the id when an unread row is clicked', async () => {
    const onActivate = vi.fn();
    render(
      <NotificationListItem notification={baseNotification} onActivate={onActivate} now={NOW} />,
    );
    await userEvent.click(screen.getByRole('button'));
    expect(onActivate).toHaveBeenCalledWith(baseNotification.id);
  });

  it('does not call onActivate when an already-read row is clicked', async () => {
    const onActivate = vi.fn();
    render(
      <NotificationListItem
        notification={{ ...baseNotification, readAt: '2026-05-01T11:30:00Z' }}
        onActivate={onActivate}
        now={NOW}
      />,
    );
    await userEvent.click(screen.getByRole('button'));
    expect(onActivate).not.toHaveBeenCalled();
  });

  it('responds to keyboard activation (Enter)', async () => {
    const onActivate = vi.fn();
    render(
      <NotificationListItem notification={baseNotification} onActivate={onActivate} now={NOW} />,
    );
    const button = screen.getByRole('button');
    button.focus();
    await userEvent.keyboard('{Enter}');
    expect(onActivate).toHaveBeenCalledWith(baseNotification.id);
  });
});
