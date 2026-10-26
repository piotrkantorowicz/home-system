import { EXPENSE_CATEGORIES } from '../types';

import type { ExpenseCategory, ImportRowInput, StatementCandidate } from '../types';

/** One candidate as the user reviews it. */
export interface ReviewRow {
  candidate: StatementCandidate;
  selected: boolean;
  category: ExpenseCategory;
  /**
   * The category is a deliberate choice: a learned suggestion the user kept, or one they picked.
   * Only these rows send `merchantKey`, so an untouched `Other` never overwrites a learned rule.
   */
  categoryChosen: boolean;
  description: string;
}

/** Who pays, applied to every row of a shared-envelope import. `null` for a personal envelope. */
export interface ImportParties {
  fundingSource: 'Individual' | 'HouseholdFunds';
  paidByPersonId: string;
  participantIds: string[];
}

const isCategory = (value: string | null): value is ExpenseCategory =>
  (EXPENSE_CATEGORIES as readonly string[]).includes(value ?? '');

/** Already-imported rows can never be sent; a possible duplicate can, after an explicit tick. */
export function isSelectable(candidate: StatementCandidate): boolean {
  return candidate.status !== 'AlreadyImported';
}

/**
 * Review rows for a preview. New rows start selected, possible duplicates unselected. When the
 * statement is previewed again, edits to a row with the same source key are kept, and its tick
 * too unless the status changed.
 */
export function reviewRows(
  candidates: StatementCandidate[],
  previous: ReviewRow[] = [],
): ReviewRow[] {
  const before = new Map(previous.map((r) => [r.candidate.sourceKey, r]));
  return candidates.map((candidate) => {
    const kept = before.get(candidate.sourceKey);
    const suggested = isCategory(candidate.suggestedCategory) ? candidate.suggestedCategory : null;
    return {
      candidate,
      // A tick carries over only while the status is the same: a row that turned into a possible
      // duplicate needs a fresh, explicit "keep both".
      selected:
        isSelectable(candidate) &&
        (kept?.candidate.status === candidate.status ? kept.selected : candidate.status === 'New'),
      category: kept?.category ?? suggested ?? 'Other',
      categoryChosen: kept?.categoryChosen ?? suggested !== null,
      description: kept?.description ?? candidate.description,
    };
  });
}

/** The selected rows as the import request sends them, in display order. */
export function importRows(rows: ReviewRow[], parties: ImportParties | null): ImportRowInput[] {
  return rows
    .filter((r) => r.selected && isSelectable(r.candidate))
    .map((r) => {
      const description = r.description.trim();
      const split = parties?.fundingSource === 'Individual';
      return {
        sourceKey: r.candidate.sourceKey,
        amount: r.candidate.amount,
        occurredOn: r.candidate.occurredOn,
        category: r.category,
        fundingSource: parties?.fundingSource ?? null,
        paidByPersonId: split ? parties.paidByPersonId : null,
        participantIds: split ? parties.participantIds : null,
        description: description.length > 0 ? description : null,
        ...(r.categoryChosen && r.candidate.merchantKey
          ? { merchantKey: r.candidate.merchantKey }
          : {}),
      };
    });
}

const ROW_KEY = /^Rows\[(\d+)\](?:\.\w+)?$/;
const ROW_DETAIL = /^Rows\[(\d+)\]: (.+)$/s;

/**
 * Per-row messages from a rejected import, keyed by source key. The API names rows by their
 * index in the request (`Rows[3].Amount`, or `Rows[3]: …` in a business-rule detail).
 */
export function rowErrors(problem: unknown, sent: ImportRowInput[]): Map<string, string[]> {
  const result = new Map<string, string[]>();
  const add = (index: number, message: string) => {
    const key = sent[index]?.sourceKey;
    if (key) result.set(key, [...(result.get(key) ?? []), message]);
  };
  if (typeof problem !== 'object' || problem === null) return result;
  if ('errors' in problem && typeof problem.errors === 'object' && problem.errors !== null) {
    for (const [field, messages] of Object.entries(problem.errors)) {
      const match = ROW_KEY.exec(field);
      if (match && Array.isArray(messages))
        messages.forEach((m) => {
          add(Number(match[1]), String(m));
        });
    }
  }
  if ('detail' in problem && typeof problem.detail === 'string') {
    const match = ROW_DETAIL.exec(problem.detail);
    if (match) add(Number(match[1]), match[2] ?? '');
  }
  return result;
}
