import { describe, expect, it } from 'vitest';
import {
  formatDayRange, formatDayShort, formatGrams, formatKcal, formatLitres, formatMoney,
  formatQuantity, formatWaterProgress, goalStatus, unitLabel,
} from './format';

const en = { lang: 'en', thinSpaceThousands: true } as const;
const enComma = { lang: 'en', thinSpaceThousands: false } as const;
const pl = { lang: 'pl', thinSpaceThousands: true } as const;
const N = ' ';

describe('numbers', () => {
  it('kcal are integers with thin-space grouping', () => {
    expect(formatKcal(2100, en)).toBe(`2${N}100`);
    expect(formatKcal(14740.4, en)).toBe(`14${N}740`);
    expect(formatKcal(2150, enComma)).toBe('2,150');
  });
  it('grams: one decimal only under 10', () => {
    expect(formatGrams(46.0, en)).toBe('46');
    expect(formatGrams(859.0, en)).toBe('859');
    expect(formatGrams(4.54, en)).toBe('4.5');
    expect(formatGrams(5.0, en)).toBe('5');
    expect(formatGrams(0, en)).toBe('0');
  });
  it('water uses litres everywhere', () => {
    expect(formatLitres(1330, en)).toBe('1.3');
    expect(formatWaterProgress(1330, 2500, en)).toBe('1.3 of 2.5 L');
    expect(formatLitres(1330, pl)).toBe('1,3');
  });
  it('money keeps two decimals and groups thousands', () => {
    expect(formatMoney('4114.65', en)).toBe(`4${N}114.65`);
    expect(formatMoney('184.36', en, { currency: 'PLN', withCurrency: true })).toBe('184.36 PLN');
    expect(formatMoney('4114.65', pl)).toBe(`4${N}114,65`);
  });
});

describe('units', () => {
  it('maps API enums', () => {
    expect(unitLabel('Gram')).toBe('g');
    expect(unitLabel('Milliliter')).toBe('ml');
    expect(unitLabel('Piece', 1)).toBe('pc');
  });
  it('scales to sensible units', () => {
    expect(formatQuantity(1200, 'Gram', en)).toBe('1.2 kg');
    expect(formatQuantity(1000, 'Milliliter', en)).toBe('1 L');
    expect(formatQuantity(8, 'Piece', en)).toBe('8 pcs');
    expect(formatQuantity(480, 'Gram', en)).toBe('480 g');
  });
});

describe('dates', () => {
  it('short day and ranges', () => {
    expect(formatDayShort('2026-10-03', 'en')).toBe('Sat 3 Oct');
    // en-GB abbreviates September as "Sep" or "Sept" depending on the ICU version
    expect(formatDayRange('2026-09-28', '2026-10-04', 'en')).toMatch(/^28 Sept? – 4 Oct$/);
  });
});

describe('goal status', () => {
  it('limits: over / on target / under', () => {
    expect(goalStatus(2200, 2100, 'limit').state).toBe('over');
    expect(goalStatus(2050, 2100, 'limit').state).toBe('onTarget');
    expect(goalStatus(2020, 2100, 'limit').state).toBe('under');
  });
  it('minimums: going over is good', () => {
    expect(goalStatus(216, 210, 'min').state).toBe('met');
    expect(goalStatus(859, 980, 'min').state).toBe('short');
  });
});
