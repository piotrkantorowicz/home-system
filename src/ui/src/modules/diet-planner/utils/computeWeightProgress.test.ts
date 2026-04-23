import { describe, it, expect } from 'vitest';

import { computeWeightProgress } from './computeWeightProgress';

describe('computeWeightProgress', () => {
  it('reports losing direction when current is below start moving toward target', () => {
    const result = computeWeightProgress({ start: 90, current: 85, target: 80 });

    expect(result.direction).toBe('losing');
    expect(result.kgRemaining).toBeCloseTo(5, 5);
    expect(result.percentComplete).toBeCloseTo(50, 5);
  });

  it('reports gaining direction when current is above start moving toward target', () => {
    const result = computeWeightProgress({ start: 60, current: 65, target: 70 });

    expect(result.direction).toBe('gaining');
    expect(result.kgRemaining).toBeCloseTo(5, 5);
    expect(result.percentComplete).toBeCloseTo(50, 5);
  });

  it('reports maintaining when delta from start is below threshold', () => {
    const result = computeWeightProgress({ start: 80, current: 80.3, target: 75 });

    expect(result.direction).toBe('maintaining');
  });

  it('clamps percent to 100 when target reached or exceeded', () => {
    const result = computeWeightProgress({ start: 90, current: 75, target: 80 });

    expect(result.percentComplete).toBe(100);
    expect(result.kgRemaining).toBe(0);
  });

  it('clamps percent to 0 when moving away from target', () => {
    const result = computeWeightProgress({ start: 80, current: 85, target: 75 });

    expect(result.percentComplete).toBe(0);
  });
});
