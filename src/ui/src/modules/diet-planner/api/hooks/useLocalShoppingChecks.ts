import { useState } from 'react';

type LocalChecks = Record<string, boolean>;

/** Check-offs the shared API refused, kept on this device per household and date range. */
export const localChecksKey = (householdId: string, from: string, to: string) =>
  `home-system-shopping-local:${householdId}:${from}:${to}`;

export const rowKey = (productId: string, unit: string) => `${productId}|${unit}`;

function read(key: string): LocalChecks {
  try {
    const raw = window.localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as LocalChecks) : {};
  } catch {
    return {};
  }
}

function write(key: string, value: LocalChecks) {
  try {
    if (Object.keys(value).length === 0) window.localStorage.removeItem(key);
    else window.localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Storage blocked or full: the row simply is not remembered locally.
  }
}

export function useLocalShoppingChecks(householdId: string, from: string, to: string) {
  const key = localChecksKey(householdId, from, to);
  const [state, setState] = useState({ key, checks: read(key) });
  // Another household or range: load its own entries (derived state, no effect needed).
  if (state.key !== key) setState({ key, checks: read(key) });
  const checks = state.key === key ? state.checks : read(key);

  const update = (fn: (c: LocalChecks) => LocalChecks) => {
    const next = fn(read(key));
    write(key, next);
    setState({ key, checks: next });
  };

  return {
    checks,
    set: (productId: string, unit: string, isChecked: boolean) => {
      update((c) => ({ ...c, [rowKey(productId, unit)]: isChecked }));
    },
    clear: (productId: string, unit: string) => {
      update((c) =>
        Object.fromEntries(Object.entries(c).filter(([k]) => k !== rowKey(productId, unit))),
      );
    },
    clearAll: () => {
      update(() => ({}));
    },
  };
}
