import { describe, expect, it } from 'vitest';

import { isIsoDate, shiftDays, todayLocal } from './dates';
import { normalizeAmount } from './money';

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
