import { createApiContext } from '../../diet-planner/utils/seed';

import type { Page } from '@playwright/test';

/**
 * Leaves the caller's current household, if any. Used to give the reserved
 * invitee identity a clean slate at the start of a cross-user spec — a
 * previous run that failed between accepting and the end-of-test leave would
 * otherwise leave them stuck in a household on the next run.
 */
export async function ensureNoHousehold(page: Page): Promise<void> {
  const api = await createApiContext(page);
  try {
    const mine = await api.get('/api/households/me');
    if (mine.status() === 404) return;
    if (!mine.ok()) {
      throw new Error(`ensureNoHousehold: GET /api/households/me returned ${mine.status()}`);
    }
    const { id } = (await mine.json()) as { id: string };
    const leave = await api.post(`/api/households/${id}/leave`);
    if (!leave.ok()) {
      throw new Error(`ensureNoHousehold: leaving ${id} returned ${leave.status()}`);
    }
  } finally {
    await api.dispose();
  }
}

interface InvitationDto {
  id: string;
  email: string | null;
  status: string;
}

/**
 * Revokes any invitation a previous, interrupted run left pending for `email`
 * in `householdId` — `InvitePersonByEmail` refuses to issue a second one
 * while one is still pending for the same household + email.
 */
export async function revokeStalePendingInvitation(
  page: Page,
  householdId: string,
  email: string,
): Promise<void> {
  const api = await createApiContext(page);
  try {
    const res = await api.get(`/api/households/${householdId}/invitations`);
    if (!res.ok()) {
      throw new Error(`revokeStalePendingInvitation: GET returned ${res.status()}`);
    }
    const invitations = (await res.json()) as InvitationDto[];
    const stale = invitations.find(
      (invitation) => invitation.email === email && invitation.status === 'Pending',
    );
    if (!stale) return;
    const del = await api.delete(`/api/households/${householdId}/invitations/${stale.id}`);
    if (!del.ok()) {
      throw new Error(`revokeStalePendingInvitation: DELETE returned ${del.status()}`);
    }
  } finally {
    await api.dispose();
  }
}

/**
 * Arranges a clean starting point for a cross-user invitation spec: the
 * owner's own household id/name, with any invitation stale from a previous
 * interrupted run revoked, and the invitee confirmed to hold no household of
 * their own. Shared by every spec exercising the reserved invitee identity,
 * since each otherwise repeats the same three API calls before it can act.
 */
export async function arrangeCleanInvitation(
  ownerPage: Page,
  inviteePage: Page,
  inviteeEmail: string,
): Promise<{ id: string; name: string }> {
  const ownerApi = await createApiContext(ownerPage);
  let household: { id: string; name: string };
  try {
    const mine = await ownerApi.get('/api/households/me');
    if (!mine.ok()) {
      throw new Error(`arrangeCleanInvitation: GET /api/households/me returned ${mine.status()}`);
    }
    household = (await mine.json()) as { id: string; name: string };
    await revokeStalePendingInvitation(ownerPage, household.id, inviteeEmail);
  } finally {
    await ownerApi.dispose();
  }
  await ensureNoHousehold(inviteePage);
  return household;
}

interface MealSlotDto {
  id: string;
  name: string;
  defaultTime: string;
}

/**
 * Creates one product, one recipe using it, and plans it as today's meal —
 * the minimal chain that surfaces a known, attributable line on the shopping
 * list, so the spec can assert the *same* item is visible to both household
 * members without asserting on the whole aggregated list.
 */
export async function seedSharedShoppingListItem(page: Page): Promise<{ productName: string }> {
  const api = await createApiContext(page);
  try {
    const schedule = await api.get('/api/v1/meal-schedule');
    const scheduleBody = schedule.ok() ? (await schedule.text()).trim() : '';
    const slots: MealSlotDto[] =
      scheduleBody === '' || scheduleBody === 'null'
        ? []
        : (JSON.parse(scheduleBody) as { slots: MealSlotDto[] }).slots;

    let slotId = slots[0]?.id;
    if (!slotId) {
      const created = await api.put('/api/v1/meal-schedule', {
        data: { slots: [{ id: null, name: 'Lunch', defaultTime: '13:00' }] },
      });
      if (!created.ok()) {
        throw new Error(
          `seedSharedShoppingListItem: creating a meal slot returned ${created.status()}`,
        );
      }
      const refetch = await api.get('/api/v1/meal-schedule');
      slotId = ((await refetch.json()) as { slots: MealSlotDto[] }).slots[0]?.id;
      if (!slotId) {
        throw new Error('seedSharedShoppingListItem: no meal slot available after seeding one');
      }
    }

    const productName = `E2E Household Product ${Date.now()}`;
    const product = await api.post('/api/v1/products', {
      data: {
        name: productName,
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
    if (!product.ok()) {
      throw new Error(
        `seedSharedShoppingListItem: creating the product returned ${product.status()}`,
      );
    }
    // Both endpoints return the new id as a bare JSON string, not `{ id }`.
    const productId = (await product.json()) as string;

    const recipe = await api.post('/api/v1/recipes', {
      data: {
        name: `E2E Household Recipe ${Date.now()}`,
        description: null,
        instructions: null,
        servings: 1,
        prepTimeMinutes: null,
        ingredients: [{ productId, amount: 200, unit: 'g' }],
      },
    });
    if (!recipe.ok()) {
      throw new Error(
        `seedSharedShoppingListItem: creating the recipe returned ${recipe.status()}`,
      );
    }
    const recipeId = (await recipe.json()) as string;

    const today = new Date().toISOString().slice(0, 10);
    const meal = await api.post('/api/v1/meals', {
      data: {
        date: today,
        mealSlotId: slotId,
        recipeId,
        servings: 1,
        notes: null,
        mealTime: null,
        sequenceOrder: null,
      },
    });
    if (!meal.ok()) {
      throw new Error(`seedSharedShoppingListItem: planning the meal returned ${meal.status()}`);
    }

    return { productName };
  } finally {
    await api.dispose();
  }
}
