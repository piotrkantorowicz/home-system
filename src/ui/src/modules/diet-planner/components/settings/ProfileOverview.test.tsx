import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { ProfileOverview } from './ProfileOverview';

const mutateAsync = vi.fn().mockResolvedValue(undefined);

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
}));
vi.mock('react-oidc-context', () => ({
  useAuth: () => ({ user: { profile: { name: 'Kamil Malinowski' } } }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
vi.mock('@modules/diet-planner/api/hooks/useProfile', () => ({
  useProfile: () => ({
    data: {
      dateOfBirth: '1990-01-01',
      activityLevel: 'ModeratelyActive',
      heightCm: 180,
      currentWeightKg: 82.4,
      targetWeightKg: 75,
    },
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({
    data: {
      dailyCalorieTarget: null,
      proteinGrams: 140,
      carbsGrams: 240,
      fatGrams: 70,
      fiberGrams: 30,
    },
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useWeightPrediction', () => ({
  useWeightPrediction: () => ({ data: null }),
}));
vi.mock('@modules/diet-planner/api/hooks/useDietReminderSettings', () => ({
  useDietReminderSettings: () => ({
    data: {
      mealRemindersEnabled: true,
      mealReminderLeadTimeMinutes: 15,
      mealMissedGraceMinutes: 30,
      waterRemindersEnabled: false,
      waterReminderIntervalMinutes: 90,
      waterWindowStart: '08:00:00',
      waterWindowEnd: '20:00:00',
      weeklySummaryEnabled: true,
      weeklySummaryDayOfWeek: 'Sunday',
      weeklySummaryTimeOfDay: '18:00:00',
      goalAlertsEnabled: true,
    },
  }),
  useUpdateDietReminderSettings: () => ({ mutateAsync, isPending: false }),
}));

describe('ProfileOverview', () => {
  it('shows the energy-model empty state until a calorie target exists', () => {
    render(<ProfileOverview onEdit={vi.fn()} />);
    expect(screen.getByText('profile.overview.energy_model_empty')).toBeInTheDocument();
  });

  it('splits the macro bar by calorie share', () => {
    render(<ProfileOverview onEdit={vi.fn()} />);
    // protein 140*4=560, carbs 240*4=960, fat 70*9=630 -> total 2150
    // protein 26%, carbs 45%, fat 29%
    expect(screen.getByText(/140 g · 26%/)).toBeInTheDocument();
    expect(screen.getByText(/240 g · 45%/)).toBeInTheDocument();
  });

  it('toggles a reminder through the update mutation', async () => {
    render(<ProfileOverview onEdit={vi.fn()} />);
    const waterSwitch = screen.getByRole('switch', {
      name: 'dietReminderSettings.water_reminders_enabled_label',
    });
    expect(waterSwitch).not.toBeChecked();
    await userEvent.click(waterSwitch);
    expect(mutateAsync).toHaveBeenCalledWith(
      expect.objectContaining({ waterRemindersEnabled: true }),
    );
  });
});
