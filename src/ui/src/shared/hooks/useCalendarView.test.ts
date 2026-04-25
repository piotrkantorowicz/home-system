import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { useCalendarView } from './useCalendarView';

const STORAGE_KEY = 'ui.calendar.view';

let store: Record<string, string>;

const stubStorage: Storage = {
  get length() {
    return Object.keys(store).length;
  },
  clear() {
    store = {};
  },
  getItem(key) {
    return store[key] ?? null;
  },
  setItem(key, value) {
    store[key] = value;
  },
  removeItem(key) {
    // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
    delete store[key];
  },
  key(index) {
    return Object.keys(store)[index] ?? null;
  },
};

describe('useCalendarView', () => {
  beforeEach(() => {
    store = {};
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      value: stubStorage,
    });
  });

  afterEach(() => {
    store = {};
  });

  it('defaults to week', () => {
    const { result } = renderHook(() => useCalendarView());
    expect(result.current[0]).toBe('week');
  });

  it('restores persisted day value', () => {
    store[STORAGE_KEY] = 'day';
    const { result } = renderHook(() => useCalendarView());
    expect(result.current[0]).toBe('day');
  });

  it('persists value when toggled', () => {
    const { result } = renderHook(() => useCalendarView());
    act(() => {
      result.current[1]('day');
    });
    expect(result.current[0]).toBe('day');
    expect(store[STORAGE_KEY]).toBe('day');
  });
});
