import { describe, expect, it } from 'vitest';

import {
  formatDayRange,
  formatDayShort,
  formatEnergy,
  formatGrams,
  formatLitres,
  formatMinor,
  formatMoney,
  formatQuantity,
  formatTime,
  formatVolume,
  formatWaterProgress,
  formatWeight,
  goalStatus,
  NO_VALUE,
  overspendMinor,
  toMinorUnits,
  unitLabel,
} from './format';

const en = { lang: 'en', thinSpaceThousands: true } as const;
const enComma = { lang: 'en', thinSpaceThousands: false } as const;
const pl = { lang: 'pl', thinSpaceThousands: false } as const;
const N = ' ';

describe('energy', () => {
  it('groups 4-digit numbers and labels the unit', () => {
    expect(formatEnergy(2100, en)).toBe(`2${N}100 kcal`);
    expect(formatEnergy(2150, enComma)).toBe('2,150 kcal');
    expect(formatEnergy(2100, pl)).toBe(`2${N}100 kcal`);
  });
  it('converts for display only when kJ is preferred, and says so', () => {
    expect(formatEnergy(2000, { ...en, energyUnit: 'kJ' })).toBe(`8${N}368 kJ`);
  });
  it('handles missing and non-finite values', () => {
    expect(formatEnergy(undefined, en)).toBe(NO_VALUE);
    expect(formatEnergy(Number.NaN, en)).toBe(NO_VALUE);
    expect(formatEnergy(Infinity, en)).toBe(NO_VALUE);
  });
});

describe('grams / weight / volume', () => {
  it('shows one decimal only under 10', () => {
    expect(formatGrams(46.0, en)).toBe('46');
    expect(formatGrams(4.54, en)).toBe('4.5');
    expect(formatGrams(5, en)).toBe('5');
    expect(formatGrams(0, en)).toBe('0');
    expect(formatGrams(4.54, pl)).toBe('4,5');
  });
  it('weight follows the preference', () => {
    expect(formatWeight(72.46, en)).toBe('72.5 kg');
    expect(formatWeight(72.46, { ...en, weightUnit: 'lb' })).toBe('159.7 lb');
    expect(formatWeight(null, en)).toBe(NO_VALUE);
  });
  it('volume follows the preference', () => {
    expect(formatVolume(250, en)).toBe('250 ml');
    expect(formatVolume(250, { ...en, volumeUnit: 'L' })).toBe('0.25 L');
    expect(formatVolume(250, { ...en, volumeUnit: 'oz' })).toBe('8 oz');
  });
  it('never shows a valid small intake as zero', () => {
    const L = { ...en, volumeUnit: 'L' } as const;
    const oz = { ...en, volumeUnit: 'oz' } as const;
    expect(formatVolume(1, L)).toBe('1 ml');
    expect(formatVolume(1, oz)).toBe('1 ml');
    expect(formatVolume(5, L)).toBe('0.01 L');
    expect(formatVolume(0, L)).toBe('0 L');
    expect(formatWaterProgress(1, 2500, en)).toBe(`1 of 2${N}500 ml`);
    expect(formatWaterProgress(1, 2500, oz)).toBe(`1 of 2${N}500 ml`);
    expect(formatWaterProgress(0, 2500, en)).toBe('0.0 of 2.5 L');
    expect(formatWaterProgress(60, 2500, en)).toBe('0.1 of 2.5 L');
  });
  it('water progress is litres, or fluid ounces when preferred', () => {
    expect(formatLitres(1330, en)).toBe('1.3');
    expect(formatWaterProgress(1330, 2500, en)).toBe('1.3 of 2.5 L');
    expect(formatWaterProgress(1330, 2500, pl, 'z')).toBe('1,3 z 2,5 L');
    expect(formatWaterProgress(1330, 2500, { ...en, volumeUnit: 'oz' })).toBe('45 of 85 oz');
    expect(formatWaterProgress(undefined, 2500, en)).toBe(NO_VALUE);
  });
});

describe('money', () => {
  it('formats decimal strings without float drift', () => {
    expect(formatMoney('4114.65', en)).toBe(`4${N}114.65`);
    expect(formatMoney('4114.65', pl)).toBe(`4${N}114,65`);
    expect(formatMoney('4114.65', enComma)).toBe('4,114.65');
    expect(formatMoney('0.10', en)).toBe('0.10');
    expect(formatMoney('1234567.89', en)).toBe(`1${N}234${N}567.89`);
  });
  it('shows the currency only when asked', () => {
    expect(formatMoney('12', en)).toBe('12.00');
    expect(formatMoney('12', en, { currency: 'PLN' })).toBe('12.00 PLN');
  });
  it('rounds half away from zero at the third decimal and keeps real minus signs', () => {
    expect(toMinorUnits('1.005')).toBe(101n);
    expect(toMinorUnits('1.004')).toBe(100n);
    expect(toMinorUnits('-1.005')).toBe(-101n);
    expect(formatMoney('-184.36', en)).toBe('−184.36');
    expect(formatMoney('-0.001', en)).toBe('0.00');
  });
  it('is exact where floats are not', () => {
    // 0.1 + 0.2 in floats is 0.30000000000000004
    expect((toMinorUnits('0.10') ?? 0n) + (toMinorUnits('0.20') ?? 0n)).toBe(30n);
    expect(formatMinor(30n, en)).toBe('0.30');
  });
  it('computes overspend in minor units', () => {
    expect(overspendMinor(100_001n, 100_000n)).toBe(1n);
    expect(overspendMinor(100_000n, 100_000n)).toBe(0n);
    expect(overspendMinor(5n, 100_000n)).toBe(0n);
  });
  it('rejects junk', () => {
    expect(toMinorUnits('abc')).toBeNull();
    expect(toMinorUnits(undefined)).toBeNull();
    expect(toMinorUnits(Number.NaN)).toBeNull();
    expect(formatMoney('abc', en)).toBe(NO_VALUE);
  });
});

describe('units', () => {
  it('labels API enums', () => {
    expect(unitLabel('Gram')).toBe('g');
    expect(unitLabel('Milliliter')).toBe('ml');
    expect(unitLabel('Piece', 1)).toBe('pc');
    expect(unitLabel('Piece', 8)).toBe('pcs');
    expect(unitLabel('Piece', 8, 'pl')).toBe('szt.');
  });
  it('picks a sensible unit and keeps fractions', () => {
    expect(formatQuantity(1200, 'Gram', en)).toBe('1.2 kg');
    expect(formatQuantity(1000, 'Milliliter', en)).toBe('1 L');
    expect(formatQuantity(8, 'Piece', en)).toBe('8 pcs');
    expect(formatQuantity(1, 'Piece', en)).toBe('1 pc');
    expect(formatQuantity(0.5, 'Piece', en)).toBe('0.5 pcs');
    expect(formatQuantity(12.5, 'Gram', en)).toBe('12.5 g');
    expect(formatQuantity(Number.NaN, 'Gram', en)).toBe(NO_VALUE);
  });
});

describe('dates', () => {
  it('treats date-only strings as local calendar days', () => {
    expect(formatDayShort('2026-10-03', 'en')).toBe('Sat 3 Oct');
    expect(formatDayShort('2026-10-03', 'en')).toBe(formatDayShort(new Date(2026, 9, 3), 'en'));
    expect(formatDayRange('2026-09-28', '2026-10-04', 'en')).toBe('28 Sep – 4 Oct');
  });
  it('crosses year boundaries without shifting', () => {
    expect(formatDayShort('2026-12-31', 'en')).toBe('Thu 31 Dec');
    expect(formatDayShort('2027-01-01', 'en')).toBe('Fri 1 Jan');
    expect(formatDayShort('2026-09-03', 'en')).toBe('Thu 3 Sep');
  });
  it('formats instants in local time and survives bad input', () => {
    const iso = new Date(2026, 9, 3, 21, 12).toISOString();
    expect(formatTime(iso, 'en')).toBe('21:12');
    expect(formatTime('nope', 'en')).toBe(NO_VALUE);
    expect(formatDayShort(undefined, 'en')).toBe(NO_VALUE);
    expect(formatDayRange('2026-09-28', 'bad', 'en')).toBe(NO_VALUE);
  });
});

describe('goalStatus', () => {
  it('limits: over is bad, inside the 3% band is on target', () => {
    expect(goalStatus(2200, 2000, 'limit').state).toBe('over');
    expect(goalStatus(2060, 2000, 'limit').state).toBe('onTarget'); // exactly +3%
    expect(goalStatus(2061, 2000, 'limit').state).toBe('over');
    expect(goalStatus(1940, 2000, 'limit').state).toBe('onTarget'); // exactly −3%
    expect(goalStatus(1900, 2000, 'limit').state).toBe('under');
  });
  it('minimums: over is met, well short is short', () => {
    expect(goalStatus(180, 150, 'min').state).toBe('met');
    expect(goalStatus(146, 150, 'min').state).toBe('met');
    expect(goalStatus(100, 150, 'min').state).toBe('short');
  });
  it('returns the signed diff', () => {
    expect(goalStatus(2200, 2000, 'limit').diff).toBe(200);
  });
  it('handles zero targets and missing data', () => {
    expect(goalStatus(0, 0, 'limit').state).toBe('onTarget');
    expect(goalStatus(5, 0, 'limit').state).toBe('over');
    expect(goalStatus(5, 0, 'min').state).toBe('met');
    expect(goalStatus(undefined, 2000, 'limit').state).toBe('none');
    expect(goalStatus(100, null, 'min').state).toBe('none');
    expect(goalStatus(Number.NaN, 100, 'limit').state).toBe('none');
  });
});
