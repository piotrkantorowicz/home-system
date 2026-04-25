import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { useSidebarCollapsed } from './useSidebarCollapsed';

const STORAGE_KEY = 'ui.sidebar.collapsed';

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

describe('useSidebarCollapsed', () => {
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

  it('defaults to false when no value persisted', () => {
    const { result } = renderHook(() => useSidebarCollapsed());

    expect(result.current[0]).toBe(false);
  });

  it('restores persisted true value on mount', () => {
    store[STORAGE_KEY] = 'true';

    const { result } = renderHook(() => useSidebarCollapsed());

    expect(result.current[0]).toBe(true);
  });

  it('persists value when toggled', () => {
    const { result } = renderHook(() => useSidebarCollapsed());

    act(() => {
      result.current[1](true);
    });

    expect(result.current[0]).toBe(true);
    expect(store[STORAGE_KEY]).toBe('true');
  });

  it('reacts to cross-tab storage events', () => {
    const { result } = renderHook(() => useSidebarCollapsed());
    expect(result.current[0]).toBe(false);

    act(() => {
      window.dispatchEvent(new StorageEvent('storage', { key: STORAGE_KEY, newValue: 'true' }));
    });

    expect(result.current[0]).toBe(true);
  });
});
