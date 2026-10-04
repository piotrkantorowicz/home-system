import { NavigationAccessContext } from '@shared/context/NavigationAccessContext';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
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

function renderItem(
  notification = baseNotification,
  onActivate = vi.fn(),
  restriction: { allowedPath: string; reason: string } | null = null,
) {
  render(
    <NavigationAccessContext value={restriction}>
      <MemoryRouter initialEntries={['/notifications']}>
        <Routes>
          <Route
            path="/notifications"
            element={
              <ul>
                <NotificationListItem
                  notification={notification}
                  onActivate={onActivate}
                  now={NOW}
                />
              </ul>
            }
          />
          <Route path="/diet-planner" element={<div>today-page</div>} />
        </Routes>
      </MemoryRouter>
    </NavigationAccessContext>,
  );
  return onActivate;
}

describe('NotificationListItem', () => {
  it('is one button holding the title, body and action, with no nested controls', () => {
    renderItem();
    const button = screen.getByRole('button');
    expect(button).toHaveTextContent('Time for lunch');
    expect(button).toHaveTextContent('Lunch is starting');
    expect(button).toHaveTextContent('actions.open_today');
    expect(button.querySelector('button, a, input')).toBeNull();
    expect(screen.getAllByRole('button')).toHaveLength(1);
  });

  it('shows an unread dot only while unread', () => {
    renderItem();
    expect(screen.getByRole('img', { name: 'inbox.unread_aria' })).toBeInTheDocument();
  });

  it('keeps read items legible: no strikethrough, no dimming, no dot', () => {
    renderItem({ ...baseNotification, readAt: '2026-05-01T11:30:00Z' });
    expect(screen.queryByRole('img', { name: 'inbox.unread_aria' })).not.toBeInTheDocument();
    const html = screen.getByRole('button').outerHTML;
    expect(html).not.toMatch(/line-through|opacity-/);
    expect(screen.getByText('Time for lunch')).toHaveClass('font-medium');
  });

  it('marks unread read and opens the destination on click', async () => {
    const onActivate = renderItem();
    await userEvent.click(screen.getByRole('button'));
    expect(onActivate).toHaveBeenCalledWith(baseNotification.id);
    expect(screen.getByText('today-page')).toBeInTheDocument();
  });

  it('opens the destination of a read item without marking it read again', async () => {
    const onActivate = renderItem({ ...baseNotification, readAt: '2026-05-01T11:30:00Z' });
    await userEvent.click(screen.getByRole('button'));
    expect(onActivate).not.toHaveBeenCalled();
    expect(screen.getByText('today-page')).toBeInTheDocument();
  });

  it('responds to keyboard activation (Enter)', async () => {
    const onActivate = renderItem();
    screen.getByRole('button').focus();
    await userEvent.keyboard('{Enter}');
    expect(onActivate).toHaveBeenCalledWith(baseNotification.id);
  });

  it('hides the action and does not navigate when the destination is not authorized', async () => {
    const onActivate = renderItem(baseNotification, vi.fn(), {
      allowedPath: '/household',
      reason: 'Join a household first',
    });
    const button = screen.getByRole('button');
    expect(button).not.toHaveTextContent('actions.open_today');
    await userEvent.click(button);
    expect(onActivate).toHaveBeenCalled();
    expect(screen.queryByText('today-page')).not.toBeInTheDocument();
  });
});
