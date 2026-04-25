import { request } from '@playwright/test';

import type { APIRequestContext, Page } from '@playwright/test';

const API_BASE_URL = process.env['API_BASE_URL'] ?? 'http://localhost:5050';

/**
 * Pulls the current access token out of the authenticated page's localStorage.
 * Works in both serial and parallel modes because the fixture keeps the
 * storage freshly refreshed — we read at test time rather than at auth-setup
 * time.
 */
async function getAccessToken(page: Page): Promise<string> {
  const token = await page.evaluate(() => {
    const entry = Object.entries(localStorage).find(([key]) =>
      key.startsWith('oidc.user:'),
    );
    if (!entry) return null;
    try {
      return (JSON.parse(entry[1]) as { access_token?: string }).access_token ?? null;
    } catch {
      return null;
    }
  });

  if (!token) {
    throw new Error(
      'Unable to seed: no OIDC token found in localStorage. Make sure the auth fixture ran before calling a seed helper.',
    );
  }

  return token;
}

/**
 * Creates a one-shot APIRequestContext authenticated as the current user.
 * Callers must `dispose()` it when done (the helpers below do this).
 */
async function createApiContext(page: Page): Promise<APIRequestContext> {
  const token = await getAccessToken(page);
  return request.newContext({
    baseURL: API_BASE_URL,
    extraHTTPHeaders: { Authorization: `Bearer ${token}` },
  });
}

// ── Profile ───────────────────────────────────────────────────────────────────

export interface ProfileSeed {
  dateOfBirth?: string;
  gender?: 'Male' | 'Female';
  heightCm?: number;
  currentWeightKg?: number;
  targetWeightKg?: number;
  activityLevel?:
    | 'Sedentary'
    | 'LightlyActive'
    | 'ModeratelyActive'
    | 'VeryActive'
    | 'ExtraActive';
}

const DEFAULT_PROFILE: Required<ProfileSeed> = {
  dateOfBirth: '1990-05-15',
  gender: 'Male',
  heightCm: 180,
  currentWeightKg: 80,
  targetWeightKg: 75,
  activityLevel: 'ModeratelyActive',
};

/**
 * Ensures a complete biometrics profile exists for the current user. Upserts
 * via POST then PUT so the call is idempotent whether or not one exists.
 */
export async function seedProfile(page: Page, overrides: ProfileSeed = {}): Promise<void> {
  const body = { ...DEFAULT_PROFILE, ...overrides };
  const api = await createApiContext(page);
  try {
    // Try create first — if one already exists, fall through to update.
    const createRes = await api.post('/api/v1/profile', { data: body });
    if (createRes.ok()) return;

    const updateRes = await api.put('/api/v1/profile', { data: body });
    if (!updateRes.ok()) {
      throw new Error(
        `seedProfile: unable to upsert profile (POST ${createRes.status()}, PUT ${updateRes.status()})`,
      );
    }
  } finally {
    await api.dispose();
  }
}

// ── Notification preferences ─────────────────────────────────────────────────

export interface NotificationPreferencesSeed {
  mealReminderEnabled?: boolean;
  mealReminderLeadTimeMinutes?: number;
  waterReminderEnabled?: boolean;
  waterReminderIntervalMinutes?: number;
  weeklySummaryEnabled?: boolean;
  goalMilestoneAlertsEnabled?: boolean;
}

const DEFAULT_NOTIFICATION_PREFS: Required<NotificationPreferencesSeed> = {
  mealReminderEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  waterReminderEnabled: false,
  waterReminderIntervalMinutes: 60,
  weeklySummaryEnabled: false,
  goalMilestoneAlertsEnabled: true,
};

export async function seedNotificationPreferences(
  page: Page,
  overrides: NotificationPreferencesSeed = {},
): Promise<void> {
  const body = { ...DEFAULT_NOTIFICATION_PREFS, ...overrides };
  const api = await createApiContext(page);
  try {
    const res = await api.put('/api/v1/notification-preferences', { data: body });
    if (!res.ok()) {
      throw new Error(
        `seedNotificationPreferences: PUT returned ${res.status()}: ${await res.text()}`,
      );
    }
  } finally {
    await api.dispose();
  }
}

// ── Meal schedule ────────────────────────────────────────────────────────────

export interface MealSlotSeed {
  name: string;
  defaultTime: string;
}

const DEFAULT_MEAL_SCHEDULE: MealSlotSeed[] = [
  { name: 'Breakfast', defaultTime: '08:00' },
  { name: 'Lunch', defaultTime: '13:00' },
  { name: 'Snack', defaultTime: '16:00' },
  { name: 'Dinner', defaultTime: '19:00' },
];

export async function seedMealSchedule(
  page: Page,
  slots: MealSlotSeed[] = DEFAULT_MEAL_SCHEDULE,
): Promise<void> {
  const api = await createApiContext(page);
  try {
    const res = await api.put('/api/v1/meal-schedule', { data: { slots } });
    if (!res.ok()) {
      throw new Error(
        `seedMealSchedule: PUT returned ${res.status()}: ${await res.text()}`,
      );
    }
  } finally {
    await api.dispose();
  }
}
