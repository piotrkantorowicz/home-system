import { describe, expect, it } from 'vitest';

import { importRows, reviewRows, rowErrors } from './imports';

import type { StatementCandidate } from '../types';

const candidate = (key: string, status: string, overrides: Partial<StatementCandidate> = {}) =>
  ({
    lineNumber: 1,
    sourceKey: key,
    occurredOn: '2026-10-18',
    amount: '12.50',
    description: `Shop ${key}`,
    merchantKey: `SHOP ${key}`,
    suggestedCategory: null,
    status,
    matches: [],
    ...overrides,
  }) satisfies StatementCandidate;

describe('reviewRows', () => {
  it('selects new rows, leaves duplicates for an explicit tick and uses a learned category', () => {
    const rows = reviewRows([
      candidate('a', 'New', { suggestedCategory: 'Health' }),
      candidate('b', 'PossibleDuplicate'),
      candidate('c', 'AlreadyImported'),
    ]);

    expect(rows.map((r) => [r.selected, r.category, r.categoryChosen])).toEqual([
      [true, 'Health', true],
      [false, 'Other', false],
      [false, 'Other', false],
    ]);
  });

  it('keeps edits for the same source key when the statement is previewed again', () => {
    const first = reviewRows([candidate('a', 'New')]).map((r) => ({
      ...r,
      description: 'Edited',
      category: 'Leisure' as const,
      categoryChosen: true,
    }));

    const again = reviewRows([candidate('a', 'New'), candidate('b', 'New')], first);

    expect(again[0]).toMatchObject({ description: 'Edited', category: 'Leisure' });
    expect(again[1]).toMatchObject({ description: 'Shop b', category: 'Other' });
  });
});

describe('importRows', () => {
  it('sends selected rows with edits, and a merchant key only for a deliberate category', () => {
    const rows = reviewRows([
      candidate('a', 'New', { suggestedCategory: 'Health' }),
      candidate('b', 'New'),
      candidate('c', 'AlreadyImported'),
    ]).map((r) => (r.candidate.sourceKey === 'b' ? { ...r, description: '  ' } : r));

    const sent = importRows(rows, {
      fundingSource: 'Individual',
      paidByPersonId: 'me',
      participantIds: ['me', 'bea'],
    });

    expect(sent).toHaveLength(2);
    expect(sent[0]).toMatchObject({ sourceKey: 'a', category: 'Health', merchantKey: 'SHOP a' });
    expect(sent[1]).not.toHaveProperty('merchantKey');
    expect(sent[1]).toMatchObject({
      sourceKey: 'b',
      description: null,
      paidByPersonId: 'me',
      participantIds: ['me', 'bea'],
    });
  });

  it('sends no payer or split for household funds or a personal envelope', () => {
    const rows = reviewRows([candidate('a', 'New')]);

    expect(
      importRows(rows, {
        fundingSource: 'HouseholdFunds',
        paidByPersonId: 'me',
        participantIds: ['me'],
      })[0],
    ).toMatchObject({
      fundingSource: 'HouseholdFunds',
      paidByPersonId: null,
      participantIds: null,
    });
    expect(importRows(rows, null)[0]).toMatchObject({ fundingSource: null, paidByPersonId: null });
  });
});

describe('rowErrors', () => {
  const sent = importRows(reviewRows([candidate('a', 'New'), candidate('b', 'New')]), null);

  it('maps validation keys and a business-rule detail to the rows that were sent', () => {
    expect(
      rowErrors({ errors: { 'Rows[1].Amount': ['Bad amount.'], AccountId: ['x'] } }, sent),
    ).toEqual(new Map([['b', ['Bad amount.']]]));
    expect(rowErrors({ detail: 'Rows[0]: Participants must be adults.' }, sent)).toEqual(
      new Map([['a', ['Participants must be adults.']]]),
    );
  });

  it('ignores anything that does not name a row', () => {
    expect(rowErrors({ detail: 'This envelope is archived.' }, sent).size).toBe(0);
    expect(rowErrors(undefined, sent).size).toBe(0);
  });
});
