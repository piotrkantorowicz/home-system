import { describe, expect, it } from 'vitest';

import { initials } from './initials';

describe('initials', () => {
  it('uses the first letters of the first and last name', () => {
    expect(initials('Paweł Kantorowicz')).toBe('PK');
    expect(initials('Ana Maria de Souza')).toBe('AS');
  });

  it('gives a single letter when there is no surname', () => {
    expect(initials('Zosia')).toBe('Z');
    expect(initials('  zosia  ')).toBe('Z');
  });

  it('is empty for a blank name', () => {
    expect(initials('   ')).toBe('');
  });
});
