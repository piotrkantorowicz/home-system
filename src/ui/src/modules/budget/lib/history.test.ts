import { describe, expect, it } from 'vitest';

import { buildTimeline, diffSnapshots } from './history';

import type { ExpenseRevision } from '../types';

type Snap = ExpenseRevision['snapshot'];
const snap = (over: Partial<Snap> = {}): Snap => ({
  amount: '100.00',
  category: 'Groceries',
  occurredOn: '2026-10-01',
  description: null,
  fundingSource: 'Individual',
  paidByPersonId: 'a',
  paidByDisplayName: 'Ania',
  addedByPersonId: 'a',
  addedByDisplayName: 'Ania',
  isVoided: false,
  shares: [
    { personId: 'a', personDisplayName: 'Ania', amount: '50.00' },
    { personId: 'b', personDisplayName: 'Bea', amount: '50.00' },
  ],
  ...over,
});
const rev = (n: number, snapshot: Snap): ExpenseRevision => ({
  revisionNumber: n,
  operation: n === 1 ? 'Create' : 'Update',
  actorPersonId: 'a',
  actorDisplayName: 'Ania',
  reason: null,
  createdAt: `2026-10-0${String(n)}T10:00:00Z`,
  snapshot,
});

describe('diffSnapshots', () => {
  it('reports no change for identical snapshots, a missing description and an empty one', () => {
    expect(diffSnapshots(snap(), snap({ description: '  ' }))).toEqual([]);
  });

  it('compares exact cents, not text', () => {
    expect(diffSnapshots(snap({ amount: '100' }), snap({ amount: '100.00' }))).toEqual([]);
    expect(diffSnapshots(snap(), snap({ amount: '100.01' }))[0]).toMatchObject({ field: 'amount' });
  });

  it('collapses equal shares into one "each share" change', () => {
    const next = snap({
      amount: '120.00',
      shares: [
        { personId: 'a', personDisplayName: 'Ania', amount: '60.00' },
        { personId: 'b', personDisplayName: 'Bea', amount: '60.00' },
      ],
    });
    expect(diffSnapshots(snap(), next)).toEqual([
      { field: 'amount', from: '100.00', to: '120.00' },
      { field: 'each_share', from: '50.00', to: '60.00' },
    ]);
  });

  it('lists per-person shares when the split is uneven, and participants when they change', () => {
    const uneven = snap({
      shares: [
        { personId: 'a', personDisplayName: 'Ania', amount: '70.00' },
        { personId: 'b', personDisplayName: 'Bea', amount: '30.00' },
      ],
    });
    expect(diffSnapshots(snap(), uneven)).toHaveLength(2);
    const solo = snap({ shares: [{ personId: 'a', personDisplayName: 'Ania', amount: '100.00' }] });
    expect(diffSnapshots(snap(), solo)).toEqual([
      { field: 'split_between', from: 'Ania, Bea', to: 'Ania' },
    ]);
  });

  it('reports payer, description and void', () => {
    const next = snap({
      paidByPersonId: 'b',
      paidByDisplayName: 'Bea',
      description: 'Milk',
      isVoided: true,
    });
    expect(diffSnapshots(snap(), next).map((c) => c.field)).toEqual([
      'description',
      'payer',
      'voided',
    ]);
  });
});

describe('buildTimeline', () => {
  it('is newest first and compares chronologically even when the API order is shuffled', () => {
    const timeline = buildTimeline([
      rev(3, snap({ amount: '90.00', shares: [] })),
      rev(1, snap()),
      rev(2, snap({ amount: '120.00' })),
    ]);
    expect(timeline.map((e) => Number(e.revision.revisionNumber))).toEqual([3, 2, 1]);
    expect(timeline[2]?.changes).toBeNull();
    expect(timeline[1]?.changes?.[0]).toEqual({ field: 'amount', from: '100.00', to: '120.00' });
    expect(timeline[0]?.changes?.[0]).toEqual({ field: 'amount', from: '120.00', to: '90.00' });
  });
});
