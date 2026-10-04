import { describe, expect, it } from 'vitest';

import { buildWaterHistory } from './waterHistory';

const dates = ['a', 'b', 'c', 'd'];

describe('buildWaterHistory', () => {
  it('counts goal days over completed days only, excluding today', () => {
    const h = buildWaterHistory(dates, [2500, 1000, null, 9999], 2500);
    expect(h.completed).toBe(3);
    expect(h.met).toBe(1);
    expect(h.missing).toBe(1);
    expect(h.days[3]?.isToday).toBe(true);
  });

  it('treats zero as nothing logged and never meets a missing goal', () => {
    const h = buildWaterHistory(dates, [0, 3000, 3000, 0], null);
    expect(h.days[0]?.totalMl).toBeNull();
    expect(h.met).toBe(0);
  });
});
