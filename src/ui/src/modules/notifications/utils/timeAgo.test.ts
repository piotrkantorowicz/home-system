import { describe, it, expect } from 'vitest';

import { formatTimeAgo } from './timeAgo';

const NOW = new Date('2026-05-01T12:00:00Z');

describe('formatTimeAgo', () => {
  it('returns "now" for sub-minute differences', () => {
    expect(formatTimeAgo('2026-05-01T11:59:30Z', 'en', NOW)).toBe('now');
  });

  it('returns minutes for sub-hour differences', () => {
    const result = formatTimeAgo('2026-05-01T11:55:00Z', 'en', NOW);
    expect(result).toContain('5');
    expect(result).toMatch(/min/i);
  });

  it('returns hours for sub-day differences', () => {
    expect(formatTimeAgo('2026-05-01T09:00:00Z', 'en', NOW)).toMatch(/3.*h/i);
  });

  it('returns days for older differences', () => {
    expect(formatTimeAgo('2026-04-28T12:00:00Z', 'en', NOW)).toMatch(/3.*d/i);
  });

  it('falls back to en when locale is unsupported', () => {
    expect(typeof formatTimeAgo('2026-04-28T12:00:00Z', 'xx-XX', NOW)).toBe('string');
  });
});
