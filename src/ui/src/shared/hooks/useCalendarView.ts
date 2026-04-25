import { useCallback, useEffect, useState } from 'react';

const STORAGE_KEY = 'ui.calendar.view';

export type CalendarView = 'week' | 'day';

function readInitial(): CalendarView {
  if (typeof window === 'undefined') return 'week';
  try {
    return window.localStorage.getItem(STORAGE_KEY) === 'day' ? 'day' : 'week';
  } catch {
    return 'week';
  }
}

export function useCalendarView(): readonly [CalendarView, (next: CalendarView) => void] {
  const [view, setViewState] = useState<CalendarView>(readInitial);

  const setView = useCallback((next: CalendarView) => {
    setViewState(next);
    try {
      window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // localStorage unavailable — fall back to in-memory only.
    }
  }, []);

  useEffect(() => {
    function onStorage(event: StorageEvent) {
      if (event.key !== STORAGE_KEY) return;
      setViewState(event.newValue === 'day' ? 'day' : 'week');
    }
    window.addEventListener('storage', onStorage);
    return () => {
      window.removeEventListener('storage', onStorage);
    };
  }, []);

  return [view, setView] as const;
}
