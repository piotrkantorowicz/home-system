import { createApiContext } from '../../diet-planner/utils/seed';

import type { Page } from '@playwright/test';

/** An API amount (`"12.5"`) in minor units. */
export const minor = (amount: string) => Math.round(Number(amount) * 100);

/**
 * A regex for a PLN amount as the UI formats it — digit groups may be separated by any
 * (no-break / thin) space, the decimal separator is a point or comma.
 */
export function plnPattern(minorUnits: number): RegExp {
  const whole = String(Math.trunc(Math.abs(minorUnits) / 100)).replace(
    /\B(?=(\d{3})+(?!\d))/g,
    '\\s?',
  );
  const cents = String(Math.abs(minorUnits) % 100).padStart(2, '0');
  return new RegExp(`${minorUnits < 0 ? '[-−]' : ''}${whole}[.,]${cents} PLN`);
}

async function json<T>(page: Page, path: string): Promise<T> {
  const api = await createApiContext(page);
  try {
    const response = await api.get(path);
    if (!response.ok()) throw new Error(`${path} returned ${String(response.status())}`);
    return (await response.json()) as T;
  } finally {
    await api.dispose();
  }
}

export async function status(page: Page, path: string): Promise<number> {
  const api = await createApiContext(page);
  try {
    return (await api.get(path)).status();
  } finally {
    await api.dispose();
  }
}

export const netWorth = (page: Page, scope = 'household') =>
  json<{ total: string; wallets: { id: string; name: string }[] }>(
    page,
    `/api/investments/net-worth?scope=${scope}`,
  );

export const netWorthHistory = (page: Page) =>
  json<{ points: { date: string; total: string }[] }>(page, '/api/investments/net-worth/history');

export const listWallets = (page: Page) =>
  json<{ items: { id: string; name: string }[] }>(
    page,
    '/api/investments/wallets?includeArchived=true&pageSize=100',
  );

// Wallets can only be archived, never deleted — remember what each page created and archive it
// afterwards so totals on the shared worker households stay stable across runs.
const created = new Map<Page, string[]>();

export function trackWallet(page: Page, id: string): void {
  created.set(page, [...(created.get(page) ?? []), id]);
}

export async function createWallet(
  page: Page,
  name: string,
  options: { kind?: string; visibility?: 'Shared' | 'Private'; balance?: string } = {},
): Promise<string> {
  const api = await createApiContext(page);
  try {
    const response = await api.post('/api/investments/wallets', {
      data: {
        name,
        institution: 'Other',
        kind: options.kind ?? 'Savings',
        currency: 'PLN',
        visibility: options.visibility ?? 'Shared',
        ownerPersonId: null,
      },
    });
    if (!response.ok()) throw new Error(`create wallet returned ${String(response.status())}`);
    const { id } = (await response.json()) as { id: string };
    trackWallet(page, id);
    if (options.balance) {
      const today = new Date().toISOString().slice(0, 10);
      const put = await api.put(`/api/investments/wallets/${id}/snapshots/${today}`, {
        data: { amount: options.balance },
      });
      if (!put.ok()) throw new Error(`record balance returned ${String(put.status())}`);
    }
    return id;
  } finally {
    await api.dispose();
  }
}

/** Archives every wallet this page created. Best effort: access may already be revoked. */
export async function archiveTrackedWallets(page: Page): Promise<void> {
  const ids = created.get(page) ?? [];
  created.delete(page);
  if (ids.length === 0) return;
  const api = await createApiContext(page);
  try {
    for (const id of ids) await api.post(`/api/investments/wallets/${id}/archive`);
  } finally {
    await api.dispose();
  }
}
