import { formatNumber, setThousandsSeparator } from '@shared/lib/utils';
import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { usePreferences, usePreferenceEffects, __resetPreferences } from './usePreferences';

const THIN = String.fromCharCode(0x2009);

beforeEach(() => {
  window.localStorage.clear();
  __resetPreferences();
  setThousandsSeparator(THIN);
  delete document.documentElement.dataset.density;
});

afterEach(() => {
  setThousandsSeparator(THIN);
});

describe('usePreferences', () => {
  it('returns defaults when nothing is stored', () => {
    const { result } = renderHook(() => usePreferences());
    expect(result.current.prefs.weekStart).toBe('monday');
    expect(result.current.prefs.thinSpaceThousands).toBe(true);
  });

  it('persists a changed value to localStorage', () => {
    const { result } = renderHook(() => usePreferences());
    act(() => {
      result.current.set('weekStart', 'sunday');
    });
    expect(result.current.prefs.weekStart).toBe('sunday');
    const raw = window.localStorage.getItem('home-system-prefs');
    expect(raw).toContain('sunday');
  });
});

describe('usePreferenceEffects', () => {
  it('switches the thousands separator to a comma when thin-space is off', () => {
    const { result } = renderHook(() => {
      usePreferenceEffects();
      return usePreferences();
    });

    expect(formatNumber(2150)).toBe(`2${THIN}150`);

    act(() => {
      result.current.set('thinSpaceThousands', false);
    });

    expect(formatNumber(2150)).toBe('2,150');
  });

  it('sets data-density=compact on the root when compact density is on', () => {
    const { result } = renderHook(() => {
      usePreferenceEffects();
      return usePreferences();
    });

    expect(document.documentElement.dataset.density).toBeUndefined();

    act(() => {
      result.current.set('compactDensity', true);
    });

    expect(document.documentElement.dataset.density).toBe('compact');
  });
});
