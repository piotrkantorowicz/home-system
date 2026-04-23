import { describe, it, expect } from 'vitest';

import { linearRegression, type RegressionResult } from './linearRegression';

function ensure(result: RegressionResult | null): RegressionResult {
  if (result === null) throw new Error('expected non-null regression result');
  return result;
}

describe('linearRegression', () => {
  it('returns null for fewer than 2 points', () => {
    expect(linearRegression([])).toBeNull();
    expect(linearRegression([{ x: 1, y: 1 }])).toBeNull();
  });

  it('fits a perfect line', () => {
    const result = ensure(
      linearRegression([
        { x: 0, y: 0 },
        { x: 1, y: 2 },
        { x: 2, y: 4 },
      ]),
    );

    expect(result.slope).toBeCloseTo(2, 5);
    expect(result.intercept).toBeCloseTo(0, 5);
  });

  it('fits a line with non-zero intercept', () => {
    const result = ensure(
      linearRegression([
        { x: 0, y: 5 },
        { x: 2, y: 9 },
        { x: 4, y: 13 },
      ]),
    );

    expect(result.slope).toBeCloseTo(2, 5);
    expect(result.intercept).toBeCloseTo(5, 5);
  });

  it('returns slope=0 for horizontal data', () => {
    const result = ensure(
      linearRegression([
        { x: 0, y: 5 },
        { x: 1, y: 5 },
        { x: 2, y: 5 },
      ]),
    );

    expect(result.slope).toBeCloseTo(0, 5);
    expect(result.intercept).toBeCloseTo(5, 5);
  });
});
