import { describe, expect, it } from 'vitest';

import { currentMonth, isIsoDate, isMonth, shiftDays, shiftMonth, todayLocal } from './dates';
import { normalizeAmount, normalizeLimit } from './money';

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
