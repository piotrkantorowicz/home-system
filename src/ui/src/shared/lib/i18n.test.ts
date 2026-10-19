import { describe, expect, it } from 'vitest';

const locales = import.meta.glob<Record<string, unknown>>(
  ['../locales/*.json', '../../modules/*/locales/*.json'],
  { eager: true, import: 'default' },
);

const isObject = (v: unknown) => typeof v === 'object' && v !== null;

describe('module locales', () => {
  // initI18n deep-merges every module into the default namespace; a key that is a string in
  // one file and an object in another silently wipes the other module's labels (#603).
  it('never use one top-level key as both a string and a nested object', () => {
    const shapes = new Map<string, Map<boolean, string>>();
    for (const [file, content] of Object.entries(locales)) {
      for (const [key, value] of Object.entries(content)) {
        const byShape = shapes.get(key) ?? new Map<boolean, string>();
        byShape.set(isObject(value), file);
        shapes.set(key, byShape);
      }
    }
    const clashes = [...shapes]
      .filter(([, byShape]) => byShape.size > 1)
      .map(([key, byShape]) => `${key}: ${[...byShape.values()].join(' vs ')}`);
    expect(clashes).toEqual([]);
  });
});
