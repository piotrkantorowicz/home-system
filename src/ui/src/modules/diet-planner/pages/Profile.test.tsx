import { render, screen } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import Profile from './Profile';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));
vi.mock('@modules/diet-planner/components/settings', () => ({
  BodyStatsForm: () => <div>body-stats-form</div>,
  DietReminderSettingsForm: () => <div>reminders-form</div>,
  EnergyModel: () => <div>energy-model</div>,
  GoalsForm: () => <div>goals-form</div>,
  HydrationConfigForm: () => <div>water-form</div>,
  MealScheduleForm: () => <div>meal-form</div>,
  WeightHistorySection: () => <div>weight-history</div>,
}));

Element.prototype.scrollIntoView = vi.fn();

function Where() {
  const { search, hash } = useLocation();
  return <div data-testid="where">{`${search}|${hash}`}</div>;
}

function renderAt(url: string) {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Profile />
      <Where />
    </MemoryRouter>,
  );
}

describe('Profile settings page', () => {
  it('renders every form on one page with an anchor per section', () => {
    renderAt('/diet-planner/profile');
    for (const text of [
      'body-stats-form',
      'weight-history',
      'goals-form',
      'meal-form',
      'water-form',
      'reminders-form',
    ]) {
      expect(screen.getByText(text)).toBeInTheDocument();
    }
    const nav = screen.getByRole('navigation', { name: 'profile.tabs' });
    expect(nav.querySelectorAll('a')).toHaveLength(5);
    expect(screen.getByRole('link', { name: 'profile.tab_water' })).toHaveAttribute(
      'href',
      '#water',
    );
  });

  it.each([
    ['goals', '#goals'],
    ['meal-schedule', '#meal-times'],
    ['hydration', '#water'],
    ['notifications', '#reminders'],
    ['body-stats', '#profile'],
    ['bogus', '#profile'],
  ])('redirects ?section=%s to %s', (section, hash) => {
    renderAt(`/diet-planner/profile?section=${section}`);
    expect(screen.getByTestId('where')).toHaveTextContent(`|${hash}`);
  });
});
