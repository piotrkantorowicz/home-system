import { useCallback, useEffect, useState } from 'react';

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

/**
 * Client-only user preferences (units, formats, density) persisted to
 * localStorage. Theme and language stay in their existing stores.
 */
export function usePreferences() {
  const [prefs, setPrefs] = useState<Preferences>(read);

  const set = useCallback(<K extends keyof Preferences>(key: K, value: Preferences[K]) => {
    setPrefs((prev) => {
      const next = { ...prev, [key]: value };
      try {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
      } catch {
        // storage unavailable — keep the value in memory only
      }
      return next;
    });
  }, []);

  useEffect(() => {
    function onStorage(event: StorageEvent) {
      if (event.key === STORAGE_KEY) setPrefs(read());
    }
    window.addEventListener('storage', onStorage);
    return () => {
      window.removeEventListener('storage', onStorage);
    };
  }, []);

  return { prefs, set } as const;
}
