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

interface RenderOptions {
  notification?: NotificationDto;
  onActivate?: (id: string) => void;
  selected?: boolean;
  onToggleSelect?: (id: string, next: boolean) => void;
}

function renderItem(options: RenderOptions = {}) {
  const {
    notification = baseNotification,
    onActivate = vi.fn(),
    selected = false,
    onToggleSelect = vi.fn(),
  } = options;

  return render(
    <NotificationListItem
      notification={notification}
      onActivate={onActivate}
      now={NOW}
      selected={selected}
      onToggleSelect={onToggleSelect}
    />,
  );
}

describe('NotificationListItem', () => {
  it('renders the title and body', () => {
    renderItem();

    expect(screen.getByText('Time for lunch')).toBeInTheDocument();
    expect(screen.getByText('Lunch is starting')).toBeInTheDocument();
  });

  it('marks unread items with aria-pressed=false and a label', () => {
    renderItem();

    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('aria-pressed', 'false');
    expect(button).toHaveAccessibleName('inbox.mark_read_aria');
  });

  it('marks read items with aria-pressed=true', () => {
    renderItem({ notification: { ...baseNotification, readAt: '2026-05-01T11:30:00Z' } });
    expect(screen.getByRole('button')).toHaveAttribute('aria-pressed', 'true');
  });

  it('calls onActivate with the id when an unread row is clicked', async () => {
    const onActivate = vi.fn();
    renderItem({ onActivate });
    await userEvent.click(screen.getByRole('button'));
    expect(onActivate).toHaveBeenCalledWith(baseNotification.id);
  });

  it('does not call onActivate when an already-read row is clicked', async () => {
    const onActivate = vi.fn();
    renderItem({
      notification: { ...baseNotification, readAt: '2026-05-01T11:30:00Z' },
      onActivate,
    });
    await userEvent.click(screen.getByRole('button'));
    expect(onActivate).not.toHaveBeenCalled();
  });

  it('responds to keyboard activation (Enter)', async () => {
    const onActivate = vi.fn();
    renderItem({ onActivate });
    const button = screen.getByRole('button');
    button.focus();
    await userEvent.keyboard('{Enter}');
    expect(onActivate).toHaveBeenCalledWith(baseNotification.id);
  });

  it('disables the checkbox when the row is already read', () => {
    renderItem({ notification: { ...baseNotification, readAt: '2026-05-01T11:30:00Z' } });
    expect(screen.getByRole('checkbox')).toBeDisabled();
  });

  it('toggles selection through the checkbox', async () => {
    const onToggleSelect = vi.fn();
    renderItem({ onToggleSelect });
    await userEvent.click(screen.getByRole('checkbox'));
    expect(onToggleSelect).toHaveBeenCalledWith(baseNotification.id, true);
  });
});
