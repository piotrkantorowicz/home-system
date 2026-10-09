import { createApiContext } from '../../diet-planner/utils/seed';

import type { APIRequestContext, Page } from '@playwright/test';

export const today = () => new Date().toISOString().slice(0, 10);

async function ok<T>(
  response: Awaited<ReturnType<APIRequestContext['get']>>,
  what: string,
): Promise<T> {
  if (!response.ok())
    throw new Error(`${what} returned ${response.status()} ${await response.text()}`);
  return (await response.json()) as T;
}

/** The caller's `PersonId`, syncing the Person row first (a fresh identity may not have one yet). */
export async function myPersonId(page: Page): Promise<string> {
  const api = await createApiContext(page);
  try {
    return (await ok<{ personId: string }>(await api.post('/api/persons/me/sync'), 'sync'))
      .personId;
  } finally {
    await api.dispose();
  }
}

/** Initialises Budget for the caller's household unless a previous run already did. */
export async function ensureBudget(page: Page): Promise<void> {
  const api = await createApiContext(page);
  try {
    if ((await api.get('/api/budget')).ok()) return;
    await ok(await api.post('/api/budget', { data: { currency: 'PLN' } }), 'initialise budget');
  } finally {
    await api.dispose();
  }
}

// Envelopes can only be archived, never deleted — remember what each page created so the test can
// archive it afterwards instead of piling up active envelopes on the shared worker accounts.
const created = new Map<Page, string[]>();

export function trackEnvelope(page: Page, id: string): void {
  created.set(page, [...(created.get(page) ?? []), id]);
}

export async function createEnvelope(
  page: Page,
  name: string,
  visibility: 'Household' | 'Personal',
  ownerPersonId: string | null = null,
): Promise<string> {
  const api = await createApiContext(page);
  try {
    const body = await ok<{ id: string }>(
      await api.post('/api/budget/accounts', { data: { name, visibility, ownerPersonId } }),
      `create envelope ${name}`,
    );
    trackEnvelope(page, body.id);
    return body.id;
  } finally {
    await api.dispose();
  }
}

/** Archives every envelope this page created. Best effort: access may already be revoked. */
export async function archiveTrackedEnvelopes(page: Page): Promise<void> {
  const ids = created.get(page) ?? [];
  created.delete(page);
  if (ids.length === 0) return;
  const api = await createApiContext(page);
  try {
    for (const id of ids) {
      const account = await api.get(`/api/budget/accounts/${id}`);
      if (!account.ok()) continue;
      const { revision, isArchived } = (await account.json()) as {
        revision: number;
        isArchived: boolean;
      };
      if (isArchived) continue;
      await api.post(`/api/budget/accounts/${id}/archive`, {
        data: { expectedRevision: revision },
      });
    }
  } finally {
    await api.dispose();
  }
}

export interface ExpenseSeed {
  accountId: string;
  amount: string;
  category?: string;
  fundingSource?: 'Individual' | 'HouseholdFunds';
  paidByPersonId?: string | null;
  participantIds?: string[] | null;
}

export async function createExpense(page: Page, seed: ExpenseSeed): Promise<{ expenseId: string }> {
  const api = await createApiContext(page);
  try {
    return await ok(
      await api.post('/api/budget/expenses', {
        data: {
          clientRequestId: crypto.randomUUID(),
          accountId: seed.accountId,
          amount: seed.amount,
          occurredOn: today(),
          category: seed.category ?? 'Other',
          fundingSource: seed.fundingSource ?? null,
          paidByPersonId: seed.paidByPersonId ?? null,
          participantIds: seed.participantIds ?? null,
        },
      }),
      'create expense',
    );
  } finally {
    await api.dispose();
  }
}

/** One GET through the caller's token; returns the status and parsed body (if JSON). */
export async function get<T = unknown>(
  page: Page,
  path: string,
): Promise<{ status: number; body: T }> {
  const api = await createApiContext(page);
  try {
    const response = await api.get(path);
    const text = await response.text();
    return { status: response.status(), body: (text ? JSON.parse(text) : null) as T };
  } finally {
    await api.dispose();
  }
}

export async function send(
  page: Page,
  method: 'post' | 'put' | 'delete',
  path: string,
  data?: unknown,
): Promise<number> {
  const api = await createApiContext(page);
  try {
    return (await api[method](path, data === undefined ? {} : { data })).status();
  } finally {
    await api.dispose();
  }
}

/** Minor units, so spending deltas compare without floats. */
export const minor = (amount: string) => Math.round(Number(amount) * 100);
