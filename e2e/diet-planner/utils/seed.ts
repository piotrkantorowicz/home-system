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
    const entry = Object.entries(localStorage).find(([key]) => key.startsWith('oidc.user:'));
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
 * Callers must `dispose()` it when done (the helpers below do this). Exported
 * so other modules' seed helpers (e.g. household) can build on the same
 * token-reading logic instead of duplicating it.
 */
export async function createApiContext(page: Page): Promise<APIRequestContext> {
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
  activityLevel?: 'Sedentary' | 'LightlyActive' | 'ModeratelyActive' | 'VeryActive' | 'ExtraActive';
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

// ── Goals ────────────────────────────────────────────────────────────────────

export interface GoalsSeed {
  dailyCalorieTarget?: number;
  proteinGrams?: number;
  carbsGrams?: number;
  fatGrams?: number;
  fiberGrams?: number;
}

const DEFAULT_GOALS: Required<GoalsSeed> = {
  dailyCalorieTarget: 2150,
  proteinGrams: 140,
  carbsGrams: 240,
  fatGrams: 70,
  fiberGrams: 30,
};

/**
 * Ensures nutrition goals exist for the current user. Upserts via POST then
 * PUT so the call is idempotent whether or not goals were set by an earlier
 * spec on the same worker.
 */
export async function seedGoals(page: Page, overrides: GoalsSeed = {}): Promise<void> {
  const body = { ...DEFAULT_GOALS, ...overrides };
  const api = await createApiContext(page);
  try {
    const createRes = await api.post('/api/v1/goals', { data: body });
    if (createRes.ok()) return;

    const updateRes = await api.put('/api/v1/goals', { data: body });
    if (!updateRes.ok()) {
      throw new Error(
        `seedGoals: unable to upsert goals (POST ${createRes.status()}, PUT ${updateRes.status()})`,
      );
    }
  } finally {
    await api.dispose();
  }
}

// ── Meals ────────────────────────────────────────────────────────────────────

interface MealEntryDto {
  id: string;
  mealSlotName: string;
}

/**
 * Deletes every meal logged on `date` (yyyy-MM-dd) in the slot called
 * `slotName`. The week grid only offers "Add meal" on an empty cell, so a spec
 * that adds through the grid must start from one — regardless of which other
 * specs on the worker imported plans into it earlier.
 */
export async function clearMealsInSlot(page: Page, date: string, slotName: string): Promise<void> {
  const api = await createApiContext(page);
  try {
    const res = await api.get('/api/v1/meals', {
      params: { from: date, to: date },
    });
    if (!res.ok()) {
      throw new Error(`clearMealsInSlot: GET returned ${res.status()}: ${await res.text()}`);
    }
    const meals = (await res.json()) as MealEntryDto[];
    for (const meal of meals) {
      if (meal.mealSlotName.toLowerCase() !== slotName.toLowerCase()) continue;
      const del = await api.delete(`/api/v1/meals/${meal.id}`);
      if (!del.ok()) {
        throw new Error(`clearMealsInSlot: DELETE ${meal.id} returned ${del.status()}`);
      }
    }
  } finally {
    await api.dispose();
  }
}

// ── Diet reminder settings ───────────────────────────────────────────────────

export interface DietReminderSettingsSeed {
  mealRemindersEnabled?: boolean;
  mealReminderLeadTimeMinutes?: number;
  mealMissedGraceMinutes?: number;
  waterRemindersEnabled?: boolean;
  waterReminderIntervalMinutes?: number;
  waterWindowStart?: string;
  waterWindowEnd?: string;
  weeklySummaryEnabled?: boolean;
  weeklySummaryDayOfWeek?: number;
  weeklySummaryTimeOfDay?: string;
  goalAlertsEnabled?: boolean;
}

const DEFAULT_DIET_REMINDER_SETTINGS: Required<DietReminderSettingsSeed> = {
  mealRemindersEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  mealMissedGraceMinutes: 30,
  waterRemindersEnabled: false,
  waterReminderIntervalMinutes: 60,
  waterWindowStart: '06:00:00',
  waterWindowEnd: '22:00:00',
  weeklySummaryEnabled: false,
  weeklySummaryDayOfWeek: 0,
  weeklySummaryTimeOfDay: '08:00:00',
  goalAlertsEnabled: true,
};

export async function seedDietReminderSettings(
  page: Page,
  overrides: DietReminderSettingsSeed = {},
): Promise<void> {
  const body = { ...DEFAULT_DIET_REMINDER_SETTINGS, ...overrides };
  const api = await createApiContext(page);
  try {
    const res = await api.put('/api/v1/diet-reminder-settings', { data: body });
    if (!res.ok()) {
      throw new Error(
        `seedDietReminderSettings: PUT returned ${res.status()}: ${await res.text()}`,
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

interface MealSlotDto {
  id: string;
  name: string;
  defaultTime: string;
}

/**
 * Seeds the meal schedule without ever deleting a slot. The backend refuses to
 * remove a slot that has logged entries, and other specs on the same worker
 * may already have planned meals into the existing slots — so existing slots
 * are renamed/retimed in place (ids preserved) and any surplus ones are kept.
 */
export async function seedMealSchedule(
  page: Page,
  slots: MealSlotSeed[] = DEFAULT_MEAL_SCHEDULE,
): Promise<void> {
  const api = await createApiContext(page);
  try {
    // GET returns a 200 with an empty / `null` body when nothing is configured.
    const current = await api.get('/api/v1/meal-schedule');
    const currentBody = current.ok() ? (await current.text()).trim() : '';
    const existing: MealSlotDto[] =
      currentBody === '' || currentBody === 'null'
        ? []
        : (JSON.parse(currentBody) as { slots: MealSlotDto[] }).slots;

    const upserts = slots.map((slot, index) => ({
      id: existing[index]?.id ?? null,
      ...slot,
    }));
    for (const extra of existing.slice(slots.length)) {
      upserts.push({
        id: extra.id,
        name: extra.name,
        defaultTime: extra.defaultTime,
      });
    }

    const res = await api.put('/api/v1/meal-schedule', {
      data: { slots: upserts },
    });
    if (!res.ok()) {
      throw new Error(`seedMealSchedule: PUT returned ${res.status()}: ${await res.text()}`);
    }
  } finally {
    await api.dispose();
  }
}

// ── Known nutrition totals ───────────────────────────────────────────────────

export interface KnownNutritionTotals {
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  fiber: number;
}

/**
 * Creates one product with round, easy-to-assert macros (`perServing`) and
 * plans it as a meal on each given date — since the recipe is exactly 100 g
 * of the product, `servings` scales a day's total linearly and predictably.
 * Pick dates no other spec seeds meals on (e.g. well outside the current
 * calendar week) so the day's total is exactly `perServing * servings`, not
 * `perServing * servings` plus whatever else landed on that date.
 */
export async function seedKnownNutritionDays(
  page: Page,
  days: ReadonlyArray<{ date: string; servings: number }>,
): Promise<KnownNutritionTotals> {
  const perServing: KnownNutritionTotals = {
    calories: 200,
    protein: 20,
    carbs: 25,
    fat: 8,
    fiber: 4,
  };
  await seedMealSchedule(page);

  const api = await createApiContext(page);
  try {
    const schedule = await api.get('/api/v1/meal-schedule');
    const { slots } = (await schedule.json()) as { slots: MealSlotDto[] };
    const slotId = slots[0]?.id;
    if (!slotId) {
      throw new Error('seedKnownNutritionDays: no meal slot available');
    }

    const product = await api.post('/api/v1/products', {
      data: {
        name: `E2E Known Nutrition Product ${Date.now()}`,
        calories: perServing.calories,
        protein: perServing.protein,
        carbs: perServing.carbs,
        fat: perServing.fat,
        fiber: perServing.fiber,
        defaultUnit: 'g',
        densityGramsPerMl: null,
        gramPerPiece: null,
      },
    });
    if (!product.ok()) {
      throw new Error(`seedKnownNutritionDays: creating the product returned ${product.status()}`);
    }
    // Product/recipe creation returns the new id as a bare JSON string.
    const productId = (await product.json()) as string;

    const recipe = await api.post('/api/v1/recipes', {
      data: {
        name: `E2E Known Nutrition Recipe ${Date.now()}`,
        description: null,
        instructions: null,
        servings: 1,
        prepTimeMinutes: null,
        ingredients: [{ productId, amount: 100, unit: 'g' }],
      },
    });
    if (!recipe.ok()) {
      throw new Error(`seedKnownNutritionDays: creating the recipe returned ${recipe.status()}`);
    }
    const recipeId = (await recipe.json()) as string;

    for (const day of days) {
      const meal = await api.post('/api/v1/meals', {
        data: {
          date: day.date,
          mealSlotId: slotId,
          recipeId,
          servings: day.servings,
          notes: null,
          mealTime: null,
          sequenceOrder: null,
        },
      });
      if (!meal.ok()) {
        throw new Error(
          `seedKnownNutritionDays: planning the meal on ${day.date} returned ${meal.status()}`,
        );
      }
    }

    return perServing;
  } finally {
    await api.dispose();
  }
}

// ── Bulk products (pagination) ───────────────────────────────────────────────

/**
 * Creates one product per name. Products search is household-wide, not
 * scoped to the caller, so give every name a shared unique prefix (e.g. a
 * timestamp) — searching for it then isolates exactly this batch from
 * whatever else other specs or workers have created.
 */
export async function seedProducts(page: Page, names: ReadonlyArray<string>): Promise<void> {
  const api = await createApiContext(page);
  try {
    for (const name of names) {
      const res = await api.post('/api/v1/products', {
        data: {
          name,
          calories: 100,
          protein: 5,
          carbs: 10,
          fat: 2,
          fiber: null,
          defaultUnit: 'g',
          densityGramsPerMl: null,
          gramPerPiece: null,
        },
      });
      if (!res.ok()) {
        throw new Error(`seedProducts: creating "${name}" returned ${res.status()}`);
      }
    }
  } finally {
    await api.dispose();
  }
}
