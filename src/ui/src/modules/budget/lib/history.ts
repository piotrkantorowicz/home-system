import { toMinor } from './money';

import type { ExpenseRevision } from '../types';

type Snapshot = ExpenseRevision['snapshot'];

export type FieldChange =
  | {
      field: 'amount' | 'description' | 'category' | 'occurredOn' | 'payer';
      from: string;
      to: string;
    }
  | { field: 'each_share'; from: string; to: string }
  | { field: 'share'; name: string; from: string; to: string }
  | { field: 'split_between'; from: string; to: string }
  | { field: 'voided' };

export interface RevisionEntry {
  revision: ExpenseRevision;
  /** `null` for the first revision, which is a summary rather than a diff. */
  changes: FieldChange[] | null;
}

const text = (value: string | null | undefined) => value?.trim() ?? '';

function shareChanges(prev: Snapshot, next: Snapshot): FieldChange[] {
  const before = new Map(prev.shares.map((s) => [s.personId, s]));
  const same =
    prev.shares.length === next.shares.length && next.shares.every((s) => before.has(s.personId));
  if (!same) {
    const names = (snap: Snapshot) => snap.shares.map((s) => s.personDisplayName).join(', ');
    return [{ field: 'split_between', from: names(prev), to: names(next) }];
  }
  const changed = next.shares.filter(
    (s) => toMinor(s.amount) !== toMinor(before.get(s.personId)?.amount ?? '0'),
  );
  if (changed.length === 0) return [];
  const equal = (snap: Snapshot) =>
    snap.shares.every((s) => toMinor(s.amount) === toMinor(snap.shares[0]?.amount ?? '0'));
  if (equal(prev) && equal(next))
    return [
      { field: 'each_share', from: prev.shares[0]?.amount ?? '', to: next.shares[0]?.amount ?? '' },
    ];
  return changed.map((s) => ({
    field: 'share',
    name: s.personDisplayName,
    from: before.get(s.personId)?.amount ?? '',
    to: s.amount,
  }));
}

/** The fields that differ between two consecutive snapshots; exact cents, a missing description equals an empty one. */
export function diffSnapshots(prev: Snapshot, next: Snapshot): FieldChange[] {
  const changes: FieldChange[] = [];
  if (toMinor(prev.amount) !== toMinor(next.amount))
    changes.push({ field: 'amount', from: prev.amount, to: next.amount });
  if (text(prev.description) !== text(next.description))
    changes.push({
      field: 'description',
      from: text(prev.description),
      to: text(next.description),
    });
  if (prev.category !== next.category)
    changes.push({ field: 'category', from: prev.category, to: next.category });
  if (prev.occurredOn !== next.occurredOn)
    changes.push({ field: 'occurredOn', from: prev.occurredOn, to: next.occurredOn });
  if ((prev.paidByPersonId ?? null) !== (next.paidByPersonId ?? null))
    changes.push({
      field: 'payer',
      from: prev.paidByDisplayName ?? '',
      to: next.paidByDisplayName ?? '',
    });
  changes.push(...shareChanges(prev, next));
  if (!prev.isVoided && next.isVoided) changes.push({ field: 'voided' });
  return changes;
}

/** Newest first, but each revision is compared with the one before it in time. */
export function buildTimeline(history: readonly ExpenseRevision[]): RevisionEntry[] {
  const chronological = [...history].sort(
    (a, b) => Number(a.revisionNumber) - Number(b.revisionNumber),
  );
  return chronological
    .map((revision, i) => {
      const prev = chronological[i - 1];
      return { revision, changes: prev ? diffSnapshots(prev.snapshot, revision.snapshot) : null };
    })
    .reverse();
}
