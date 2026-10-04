import { describe, expect, it } from 'vitest';

import { currentMonth, isIsoDate, isMonth, shiftDays, shiftMonth, todayLocal } from './dates';
import { minorToDecimal, normalizeAmount, normalizeLimit, splitEqual } from './money';

describe('normalizeAmount', () => {
  it.each([
    ['12', '12'],
    ['12.5', '12.5'],
    ['12,50', '12.50'],
    [' 0.01 ', '0.01'],
    ['9999999999.99', '9999999999.99'],
  ])('accepts %s', (input, expected) => {
    expect(normalizeAmount(input)).toBe(expected);
  });

  it.each(['', '0', '0.00', '-1', '+1', '1.234', '1e3', '1 000', '.5', '1.', 'abc', '12345678901'])(
    'rejects %s',
    (input) => {
      expect(normalizeAmount(input)).toBeNull();
    },
  );
});

describe('dates', () => {
  it('uses the local calendar day, not UTC', () => {
    // 23:30 local on 1 Oct stays 1 Oct even though it may already be 2 Oct in UTC.
    expect(todayLocal(new Date(2026, 9, 1, 23, 30))).toBe('2026-10-01');
  });

  it('shifts across month and year boundaries with date parts only', () => {
    expect(shiftDays('2026-10-01', -2)).toBe('2026-09-29');
    expect(shiftDays('2026-12-31', 2)).toBe('2027-01-02');
    expect(shiftDays('2028-03-01', -1)).toBe('2028-02-29');
  });

  it('validates real calendar dates', () => {
    expect(isIsoDate('2026-02-28')).toBe(true);
    expect(isIsoDate('2026-02-30')).toBe(false);
    expect(isIsoDate('26-02-28')).toBe(false);
    expect(isIsoDate('')).toBe(false);
  });
});

describe('months', () => {
  it('moves across year ends and leap years with date parts only', () => {
    expect(shiftMonth('2026-12', 1)).toBe('2027-01');
    expect(shiftMonth('2027-01', -1)).toBe('2026-12');
    expect(shiftMonth('2028-03', -1)).toBe('2028-02');
    expect(shiftMonth('2026-10', 0)).toBe('2026-10');
    expect(shiftMonth('2026-01', -13)).toBe('2024-12');
  });

  it('formats the local current month and validates months', () => {
    expect(currentMonth(new Date(2026, 9, 31, 23, 59))).toBe('2026-10');
    expect(isMonth('2026-02')).toBe(true);
    expect(isMonth('2026-13')).toBe(false);
    expect(isMonth('2026-2')).toBe(false);
  });
});

describe('normalizeLimit', () => {
  it.each([
    ['0', '0'],
    ['0,00', '0.00'],
    ['25.5', '25.5'],
  ])('accepts %s', (input, expected) => {
    expect(normalizeLimit(input)).toBe(expected);
  });

  it.each(['', '-1', '1.005', '1e2', 'abc'])('rejects %s', (input) => {
    expect(normalizeLimit(input)).toBeNull();
  });
});

describe('splitEqual', () => {
  const [a, b, c] = [
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000002',
    '7fffffff-0000-0000-0000-000000000003',
  ] as const;

  it('gives the remainder cents to the first ids in ascending order, whatever the tick order', () => {
    for (const order of [
      [a, b, c],
      [c, b, a],
      [b, c, a],
    ]) {
      const shares = splitEqual(10000, order);
      expect(shares.get(a)).toBe(3334);
      expect(shares.get(b)).toBe(3333);
      expect(shares.get(c)).toBe(3333);
      expect([...shares.values()].reduce((x, y) => x + y, 0)).toBe(10000);
    }
  });

  it('spreads several remainder cents one each', () => {
    const one = splitEqual(1000, [a, b, c]);
    expect([one.get(a), one.get(b), one.get(c)]).toEqual([334, 333, 333]);
    const two = splitEqual(1001, [a, b, c]);
    expect([two.get(a), two.get(b), two.get(c)]).toEqual([334, 334, 333]);
  });

  it('is exact for one person and empty for none', () => {
    expect(splitEqual(18436, [a]).get(a)).toBe(18436);
    expect(splitEqual(100, []).size).toBe(0);
  });

  it('compares ids case-insensitively like the canonical text form', () => {
    const upper = 'FFFFFFFF-0000-0000-0000-000000000001';
    const shares = splitEqual(101, [upper, a]);
    expect(shares.get(a)).toBe(51);
    expect(shares.get(upper)).toBe(50);
  });
});

describe('minorToDecimal', () => {
  it.each([
    [18436, '184.36'],
    [5, '0.05'],
    [100, '1.00'],
    [0, '0.00'],
    [-4050, '-40.50'],
  ])('writes %i as %s', (minor, expected) => {
    expect(minorToDecimal(minor)).toBe(expected);
  });
});
