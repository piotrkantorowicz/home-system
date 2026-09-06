import { describe, it, expect } from 'vitest';

import { formatNumber, formatSigned, getInitials } from './utils';

const THIN = String.fromCharCode(0x2009);

describe('formatNumber', () => {
  it('inserts thin-space thousands separators', () => {
    expect(formatNumber(2150)).toBe(`2${THIN}150`);
    expect(formatNumber(1240000)).toBe(`1${THIN}240${THIN}000`);
    expect(formatNumber(42)).toBe('42');
  });

  it('rounds and uses a real minus sign', () => {
    expect(formatNumber(-1234.6)).toBe(`−1${THIN}235`);
  });
});

describe('formatSigned', () => {
  it('prefixes + / − and returns 0 unsigned', () => {
    expect(formatSigned(200)).toBe('+200');
    expect(formatSigned(-130)).toBe('−130');
    expect(formatSigned(0)).toBe('0');
  });
});

describe('getInitials', () => {
  it('takes first + last word initials', () => {
    expect(getInitials('Kamil Malinowski')).toBe('KM');
  });

  it('falls back for a single word or empty input', () => {
    expect(getInitials('ada')).toBe('AD');
    expect(getInitials('   ')).toBe('?');
  });
});
