import { useCallback, useEffect, useState } from 'react';

const STORAGE_KEY = 'ui.sidebar.collapsed';

function readInitial(): boolean {
  if (typeof window === 'undefined') return false;
  try {
    return window.localStorage.getItem(STORAGE_KEY) === 'true';
  } catch {
    return false;
  }
}

export function useSidebarCollapsed(): readonly [boolean, (next: boolean) => void] {
  const [collapsed, setCollapsedState] = useState<boolean>(readInitial);

  const setCollapsed = useCallback((next: boolean) => {
    setCollapsedState(next);
    try {
      window.localStorage.setItem(STORAGE_KEY, String(next));
    } catch {
      // localStorage unavailable (private mode, quota) — silently fall back to in-memory only.
    }
  }, []);

  // Listen for cross-tab updates so multiple browser tabs stay in sync.
  useEffect(() => {
    function onStorage(event: StorageEvent) {
      if (event.key !== STORAGE_KEY) return;
      setCollapsedState(event.newValue === 'true');
    }
    window.addEventListener('storage', onStorage);
    return () => {
      window.removeEventListener('storage', onStorage);
    };
  }, []);

  return [collapsed, setCollapsed] as const;
}
