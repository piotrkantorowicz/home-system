import { describe, expect, it } from 'vitest';

import { change, normalizeAmount } from './money';

describe('normalizeAmount', () => {
  it.each([
    ['12450.10', '12450.10'],
    ['12450,1', '12450.1'],
    [' 12 450,10 ', '12450.10'],
    ['0', '0'],
  ])('accepts %s as %s', (text, expected) => {
    expect(normalizeAmount(text)).toBe(expected);
  });

  it.each(['-1', '1.234', '1e3', '1,2,3', 'abc', '', '12345678901'])('rejects %s', (text) => {
    expect(normalizeAmount(text)).toBeNull();
  });
});

describe('change', () => {
  it('is the exact difference from the previous balance', () => {
    expect(change('1200.10', '1000.25')).toBe('199.85');
    expect(change('900.00', '1000.25')).toBe('-100.25');
  });

  it('is null for the first balance', () => {
    expect(change('100.00', null)).toBeNull();
  });
});
