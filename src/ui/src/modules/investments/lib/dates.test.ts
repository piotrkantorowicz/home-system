import { describe, expect, it } from 'vitest';

import { todayInWarsaw } from './dates';

describe('todayInWarsaw', () => {
  it('uses the Warsaw date, not UTC or the browser zone', () => {
    // 23:30 UTC on 1 Oct is already 2 Oct in Warsaw (UTC+2 in summer time).
    expect(todayInWarsaw(new Date('2026-10-01T23:30:00Z'))).toBe('2026-10-02');
    expect(todayInWarsaw(new Date('2026-10-01T21:30:00Z'))).toBe('2026-10-01');
  });
});
