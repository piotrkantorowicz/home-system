import { describe, expect, it } from 'vitest';

import { changeSinceJanuary, isStale } from './netWorth';

describe('isStale', () => {
  it('flags a balance only when it is older than 45 days', () => {
    expect(isStale('2026-08-27', '2026-10-11')).toBe(false);
    expect(isStale('2026-08-26', '2026-10-11')).toBe(true);
  });
});

describe('changeSinceJanuary', () => {
  const points = [
    { date: '2025-11-30', total: '900.00', incomplete: false },
    { date: '2025-12-31', total: '1000.00', incomplete: false },
    { date: '2026-10-11', total: '1250.50', incomplete: false },
  ];

  it('is the change from 31 December with a percent', () => {
    expect(changeSinceJanuary(points, '2026-10-11', '1250.50')).toEqual({
      amount: '250.50',
      percent: 25.1,
    });
  });

  it('can be negative', () => {
    expect(changeSinceJanuary(points, '2026-10-11', '800.00')).toEqual({
      amount: '-200.00',
      percent: -20,
    });
  });

  it('is unknown without a complete 31 December point', () => {
    expect(changeSinceJanuary(points.slice(2), '2026-10-11', '1')).toBeNull();
    expect(
      changeSinceJanuary(
        [{ date: '2025-12-31', total: '5.00', incomplete: true }],
        '2026-10-11',
        '1',
      ),
    ).toBeNull();
  });

  it('has no percent from a zero baseline', () => {
    expect(
      changeSinceJanuary(
        [{ date: '2025-12-31', total: '0.00', incomplete: false }],
        '2026-10-11',
        '10.00',
      ),
    ).toEqual({ amount: '10.00', percent: null });
  });
});
