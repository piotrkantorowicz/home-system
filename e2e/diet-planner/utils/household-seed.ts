import { createApiContext } from './seed';

import type { Page } from '@playwright/test';

export interface ManagedMember {
  householdId: string;
  personId: string;
}

/**
 * Adds a managed (account-less) Child to the worker's household — the worker created the
 * household in setup, so it is the Owner — and gives the child a one-slot meal schedule.
 * Pair with `removeManagedMember` in `afterAll`: the household is shared by every spec on
 * this worker.
 */
export async function addManagedChild(
  page: Page,
  displayName: string,
  slotName: string,
): Promise<ManagedMember> {
  const api = await createApiContext(page);
  try {
    const mine = await api.get('/api/households/me');
    if (!mine.ok()) throw new Error(`addManagedChild: GET household returned ${mine.status()}`);
    const householdId = ((await mine.json()) as { id: string }).id;

    const created = await api.post(`/api/households/${householdId}/managed-members`, {
      data: { displayName, email: null, role: 'Child', nickname: null },
    });
    if (created.status() !== 201) {
      throw new Error(`addManagedChild: POST managed member returned ${created.status()}`);
    }
    const personId = (created.headers()['location'] ?? '').split('/').pop() ?? '';

    const schedule = await api.put(`/api/v1/meal-schedule?personId=${personId}`, {
      data: { slots: [{ id: null, name: slotName, defaultTime: '07:30' }] },
    });
    if (!schedule.ok()) {
      throw new Error(`addManagedChild: PUT child schedule returned ${schedule.status()}`);
    }

    return { householdId, personId };
  } finally {
    await api.dispose();
  }
}

/** Deletes the member's planned meals, then removes them from the household. */
export async function removeManagedMember(page: Page, member: ManagedMember): Promise<void> {
  const api = await createApiContext(page);
  try {
    const meals = await api.get(`/api/v1/meals?personId=${member.personId}`);
    if (meals.ok()) {
      for (const meal of (await meals.json()) as { id: string }[]) {
        await api.delete(`/api/v1/meals/${meal.id}`);
      }
    }
    await api.delete(`/api/households/${member.householdId}/members/${member.personId}`);
  } finally {
    await api.dispose();
  }
}

/** Creates a one-ingredient recipe named `name`. */
export async function seedRecipe(page: Page, name: string): Promise<void> {
  const api = await createApiContext(page);
  try {
    const product = await api.post('/api/v1/products', {
      data: {
        name: `${name} base`,
        calories: 150,
        protein: 5,
        carbs: 25,
        fat: 3,
        fiber: 2,
        defaultUnit: 'g',
        densityGramsPerMl: null,
        gramPerPiece: null,
      },
    });
    if (!product.ok()) throw new Error(`seedRecipe: POST product returned ${product.status()}`);
    const productId = (await product.json()) as string;

    const recipe = await api.post('/api/v1/recipes', {
      data: {
        name,
        description: null,
        instructions: null,
        servings: 1,
        prepTimeMinutes: null,
        ingredients: [{ productId, amount: 100, unit: 'g' }],
      },
    });
    if (!recipe.ok()) throw new Error(`seedRecipe: POST recipe returned ${recipe.status()}`);
  } finally {
    await api.dispose();
  }
}
