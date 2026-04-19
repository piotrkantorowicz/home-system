/**
 * Tests that each settings page shows a success toast on save and an error
 * toast when the mutation fails. The inline isSuccess paragraph has been
 * removed, so we also verify it is no longer rendered.
 */
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, it, expect } from 'vitest';

import { server } from '@/test/mocks/server';
import { ToastProvider } from '@shared/context/ToastContext';

import { createWrapper } from '../../../test/utils/queryWrapper';

import Goals from './Goals';
import Hydration from './Hydration';
import MealSchedule from './MealSchedule';
import NotificationPreferences from './NotificationPreferences';
import Profile from './Profile';

import type { ReactNode } from 'react';

// ── i18n stub ─────────────────────────────────────────────────────────────────
// react-i18next is mocked globally: t(key) → key, so toast assertions use
// the translation key string as the expected text.
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
  Trans: ({ i18nKey }: { i18nKey: string }) => i18nKey,
}));

const BASE = 'http://localhost:5000';

// ── render helper ─────────────────────────────────────────────────────────────
function renderPage(ui: ReactNode) {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>{ui}</ToastProvider>
    </Wrapper>,
  );
}

// ── Goals ─────────────────────────────────────────────────────────────────────
describe('Goals page — toast feedback', () => {
  it('shows success toast after saving goals', async () => {
    renderPage(<Goals />);

    // Wait for the useEffect reset to populate the calorie input with loaded data
    const input = await screen.findByDisplayValue('2000');

    await userEvent.clear(input);
    await userEvent.type(input, '2200');

    await userEvent.click(screen.getByRole('button', { name: /goals.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('goals.save_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when saving goals fails', async () => {
    server.use(http.put(`${BASE}/api/v1/goals`, () => HttpResponse.json({}, { status: 500 })));

    renderPage(<Goals />);

    const input = await screen.findByDisplayValue('2000');
    await userEvent.clear(input);
    await userEvent.type(input, '1800');

    await userEvent.click(screen.getByRole('button', { name: /goals.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('goals.save_error')).toBeInTheDocument();
    });
  });

  it('does not render the inline isSuccess paragraph', async () => {
    renderPage(<Goals />);
    await screen.findByLabelText('goals.calories_label');
    expect(screen.queryByText('goals.save_success')).not.toBeInTheDocument();
  });
});

// ── Profile ───────────────────────────────────────────────────────────────────
describe('Profile page — toast feedback', () => {
  it('shows success toast after saving profile', async () => {
    renderPage(<Profile />);

    const input = await screen.findByLabelText('profile.height');
    await userEvent.clear(input);
    await userEvent.type(input, '182');

    await userEvent.click(screen.getByRole('button', { name: /profile.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('profile.save_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when saving profile fails', async () => {
    server.use(http.put(`${BASE}/api/v1/profile`, () => HttpResponse.json({}, { status: 500 })));

    renderPage(<Profile />);

    const input = await screen.findByLabelText('profile.height');
    await userEvent.clear(input);
    await userEvent.type(input, '170');

    await userEvent.click(screen.getByRole('button', { name: /profile.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('profile.save_error')).toBeInTheDocument();
    });
  });

  it('does not render the inline isSuccess paragraph', async () => {
    renderPage(<Profile />);
    await screen.findByLabelText('profile.height');
    expect(screen.queryByText('profile.save_success')).not.toBeInTheDocument();
  });
});

// ── NotificationPreferences ───────────────────────────────────────────────────
describe('NotificationPreferences page — toast feedback', () => {
  it('shows success toast after saving preferences', async () => {
    renderPage(<NotificationPreferences />);

    // Wait for load, make form dirty by toggling a checkbox
    const checkbox = await screen.findByLabelText('notifications.meal_reminder_enabled_label');
    await userEvent.click(checkbox);

    await userEvent.click(screen.getByRole('button', { name: /notifications.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('notifications.save_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when saving preferences fails', async () => {
    server.use(
      http.put(`${BASE}/api/v1/notification-preferences`, () =>
        HttpResponse.json({}, { status: 500 }),
      ),
    );

    renderPage(<NotificationPreferences />);

    const checkbox = await screen.findByLabelText('notifications.meal_reminder_enabled_label');
    await userEvent.click(checkbox);

    await userEvent.click(screen.getByRole('button', { name: /notifications.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('notifications.save_error')).toBeInTheDocument();
    });
  });

  it('does not render the inline isSuccess paragraph', async () => {
    renderPage(<NotificationPreferences />);
    await screen.findByLabelText('notifications.meal_reminder_enabled_label');
    expect(screen.queryByText('notifications.save_success')).not.toBeInTheDocument();
  });
});

// ── MealSchedule ──────────────────────────────────────────────────────────────
/** Add a new valid slot to the MealSchedule form to make it dirty. */
async function addValidSlot() {
  await screen.findByDisplayValue('Breakfast');
  await userEvent.click(screen.getByRole('button', { name: /meal_schedule.add_slot/i }));

  // The new slot has an empty name which fails Zod min(1). Fill it using
  // fireEvent.input — react-hook-form subscribes to 'input' events internally.
  const nameInputs = screen.getAllByPlaceholderText('meal_schedule.slot_name');
  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  const newInput = nameInputs[nameInputs.length - 1]!;
  fireEvent.input(newInput, { target: { value: 'Snack' } });
}

describe('MealSchedule page — toast feedback', () => {
  it('shows success toast after saving schedule', async () => {
    renderPage(<MealSchedule />);

    await addValidSlot();

    await userEvent.click(screen.getByRole('button', { name: /meal_schedule.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_schedule.save_success')).toBeInTheDocument();
    });
  });

  it('shows error toast when saving schedule fails', async () => {
    server.use(
      http.put(`${BASE}/api/v1/meal-schedule`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage(<MealSchedule />);

    await addValidSlot();

    await userEvent.click(screen.getByRole('button', { name: /meal_schedule.save_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('meal_schedule.save_error')).toBeInTheDocument();
    });
  });

  it('does not render the inline isSuccess paragraph', async () => {
    renderPage(<MealSchedule />);
    await screen.findByDisplayValue('Breakfast');
    expect(screen.queryByText('meal_schedule.save_success')).not.toBeInTheDocument();
  });
});

// ── Hydration settings ────────────────────────────────────────────────────────
describe('Hydration page — settings form toast feedback', () => {
  it('shows success toast after saving hydration settings', async () => {
    renderPage(<Hydration />);

    const targetInput = await screen.findByLabelText('hydration.daily_target_label');
    await userEvent.clear(targetInput);
    await userEvent.type(targetInput, '3000');

    await userEvent.click(screen.getByRole('button', { name: /hydration.save_settings_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('hydration.settings_saved')).toBeInTheDocument();
    });
  });

  it('shows error toast when saving hydration settings fails', async () => {
    server.use(
      http.put(`${BASE}/api/v1/hydration/config`, () => HttpResponse.json({}, { status: 500 })),
    );

    renderPage(<Hydration />);

    const targetInput = await screen.findByLabelText('hydration.daily_target_label');
    await userEvent.clear(targetInput);
    await userEvent.type(targetInput, '2000');

    await userEvent.click(screen.getByRole('button', { name: /hydration.save_settings_btn/i }));

    await waitFor(() => {
      expect(screen.getByText('hydration.settings_save_error')).toBeInTheDocument();
    });
  });

  it('does not render the inline isSuccess paragraph', async () => {
    renderPage(<Hydration />);
    await screen.findByLabelText('hydration.daily_target_label');
    expect(screen.queryByText('hydration.settings_saved')).not.toBeInTheDocument();
  });
});
