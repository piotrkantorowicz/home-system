import { describe, expect, it } from 'vitest';

import en from './en.json';
import pl from './pl.json';

function flatten(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) return [prefix];
  return Object.entries(value).flatMap(([key, child]) =>
    flatten(child, prefix ? `${prefix}.${key}` : key),
  );
}

// Polish needs extra plural forms (few/many); every other key must exist in both languages.
const PLURAL_SUFFIX = /_(zero|one|two|few|many|other)$/;
const base = (key: string) => key.replace(PLURAL_SUFFIX, '');

describe('Budget locales', () => {
  it('has the same keys in English and Polish', () => {
    const english = new Set(flatten(en).map(base));
    const polish = new Set(flatten(pl).map(base));
    expect([...english].filter((k) => !polish.has(k))).toEqual([]);
    expect([...polish].filter((k) => !english.has(k))).toEqual([]);
  });
});
