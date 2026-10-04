import { describe, expect, it } from 'vitest';

import { buildWeekHistory } from './weekHistory';

const row = (date: string, calories: number, protein = 100) => ({
  date,
  calories,
  protein,
  carbs: 0,
  fat: 0,
  fiber: 0,
});

describe('buildWeekHistory', () => {
  it('builds seven days ending today across a month boundary', () => {
    const h = buildWeekHistory([], '2026-10-03', 2000);
    expect(h.days.map((d) => d.date)).toEqual([
      '2026-09-27',
      '2026-09-28',
      '2026-09-29',
      '2026-09-30',
      '2026-10-01',
      '2026-10-02',
      '2026-10-03',
    ]);
    expect(h.days[6]?.isToday).toBe(true);
    expect(h.avgCalories).toBeNull();
  });

  it('averages full days only, excluding today', () => {
    const h = buildWeekHistory(
      [row('2026-10-01', 1800), row('2026-10-02', 2400), row('2026-10-03', 300)],
      '2026-10-03',
      2000,
    );
    expect(h.fullDays).toBe(2);
    expect(h.avgCalories).toBe(2100);
    expect(h.avgProtein).toBe(100);
  });

  it('distinguishes a logged zero from a missing day', () => {
    const h = buildWeekHistory([row('2026-10-02', 0)], '2026-10-03', 2000);
    expect(h.days[5]?.calories).toBe(0);
    expect(h.days[4]?.calories).toBeNull();
    expect(h.fullDays).toBe(1);
    expect(h.avgCalories).toBe(0);
  });

  it('counts over-target days, honouring the 3% tolerance, and none without a target', () => {
    const data = [row('2026-10-01', 2050), row('2026-10-02', 2100)];
    expect(buildWeekHistory(data, '2026-10-03', 2000).overDays).toBe(1);
    expect(buildWeekHistory(data, '2026-10-03', null).overDays).toBe(0);
  });
});
