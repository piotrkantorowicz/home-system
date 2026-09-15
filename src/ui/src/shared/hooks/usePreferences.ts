import { setThousandsSeparator, THIN_SPACE_SEPARATOR } from '@shared/lib/utils';
import { useEffect, useSyncExternalStore } from 'react';

const STORAGE_KEY = 'home-system-prefs';

export interface Preferences {
  energyUnit: 'kcal' | 'kJ';
  weightUnit: 'kg' | 'lb';
  volumeUnit: 'ml' | 'L' | 'oz';
  weekStart: 'monday' | 'sunday';
  compactDensity: boolean;
  thinSpaceThousands: boolean;
}

const DEFAULTS: Preferences = {
  energyUnit: 'kcal',
  weightUnit: 'kg',
  volumeUnit: 'ml',
  weekStart: 'monday',
  compactDensity: false,
  thinSpaceThousands: true,
};

function read(): Preferences {
  if (typeof window === 'undefined') return DEFAULTS;
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULTS;
    return { ...DEFAULTS, ...(JSON.parse(raw) as Partial<Preferences>) };
  } catch {
    return DEFAULTS;
  }
}

// Single shared snapshot so every `usePreferences()` caller — page and the
// `usePreferenceEffects()` in the shell — reacts to the same state.
let snapshot: Preferences = read();
const listeners = new Set<() => void>();

function emit(): void {
  for (const l of listeners) l();
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

function getSnapshot(): Preferences {
  return snapshot;
}

function setPreference<K extends keyof Preferences>(key: K, value: Preferences[K]): void {
  snapshot = { ...snapshot, [key]: value };
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(snapshot));
  } catch {
    // storage unavailable — keep the value in memory only
  }
  emit();
}

if (typeof window !== 'undefined') {
  window.addEventListener('storage', (event) => {
    if (event.key === STORAGE_KEY) {
      snapshot = read();
      emit();
    }
  });
}

/**
 * Client-only user preferences (units, formats, density) persisted to
 * localStorage. Theme and language stay in their existing stores.
 */
export function usePreferences() {
  const prefs = useSyncExternalStore(subscribe, getSnapshot, getSnapshot);

  function set<K extends keyof Preferences>(key: K, value: Preferences[K]) {
    setPreference(key, value);
  }

  return { prefs, set } as const;
}

/**
 * Applies the client preferences that live outside React state — the thousands
 * separator used by `formatNumber` and the `data-density` attribute on the
 * document root. Mount once, high in the tree (the app shell).
 */
export function usePreferenceEffects(): void {
  const { prefs } = usePreferences();

  useEffect(() => {
    setThousandsSeparator(prefs.thinSpaceThousands ? THIN_SPACE_SEPARATOR : ',');
  }, [prefs.thinSpaceThousands]);

  useEffect(() => {
    const root = document.documentElement;
    if (prefs.compactDensity) root.dataset.density = 'compact';
    else delete root.dataset.density;
  }, [prefs.compactDensity]);
}

/** Test helper — reset the shared snapshot to defaults. */
export function __resetPreferences(): void {
  snapshot = { ...DEFAULTS };
  emit();
}
