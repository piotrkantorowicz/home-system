import { describe, expect, it } from 'vitest';

import { limitStatus } from './limits';

describe('limitStatus', () => {
  it('is exactly over by one cent', () => {
    expect(limitStatus('10.01', '10.00')).toMatchObject({ isOver: true, over: 1n });
  });
  it('is not over at the limit', () => {
    expect(limitStatus('10.00', '10.00')).toMatchObject({ isOver: false, left: 0n, percent: 100 });
  });
  it('treats a zero limit as a real limit', () => {
    expect(limitStatus('0.00', '0.00')).toMatchObject({ isOver: false, percent: 0 });
    expect(limitStatus('5.00', '0.00')).toMatchObject({ isOver: true, over: 500n, percent: 100 });
  });
  it('places the limit tick inside the bar when over', () => {
    expect(limitStatus('400.00', '200.00')?.limitAt).toBe(50);
  });
});
