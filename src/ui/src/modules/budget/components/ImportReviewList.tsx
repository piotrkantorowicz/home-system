import { Checkbox, Input, MoneyText, Select, StatusPill } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { useTranslation } from 'react-i18next';

import { isSelectable } from '../lib/imports';
import { EXPENSE_CATEGORIES } from '../types';

import type { ReviewRow } from '../lib/imports';
import type { ExpenseCategory } from '../types';

const MAX_DESCRIPTION = 80;

const statusVariant = {
  New: 'good',
  PossibleDuplicate: 'over',
  AlreadyImported: 'neutral',
} as const;

export interface ImportReviewListProps {
  rows: ReviewRow[];
  errors: Map<string, string[]>;
  onChange: (sourceKey: string, change: Partial<ReviewRow>) => void;
}

/** One line per candidate: stacked on a phone, a single row from `md` up. */
export function ImportReviewList({ rows, errors, onChange }: ImportReviewListProps) {
  const { t } = useTranslation('budget');
  const { dayShort } = useFormat();

  return (
    <ul className="divide-border divide-y">
      {rows.map((row) => {
        const { candidate } = row;
        const key = candidate.sourceKey;
        const selectable = isSelectable(candidate);
        const rowErrors = errors.get(key) ?? [];
        const status = candidate.status as keyof typeof statusVariant;
        const match = candidate.matches[0];
        return (
          <li key={key} className="py-3" aria-label={candidate.description}>
            <div className="flex flex-wrap items-center gap-3">
              <Checkbox
                aria-label={t('import_include_row', { description: candidate.description })}
                checked={row.selected}
                disabled={!selectable}
                onChange={(event) => {
                  onChange(key, { selected: event.target.checked });
                }}
              />
              <span className="text-text-2 text-meta w-20">{dayShort(candidate.occurredOn)}</span>
              <Input
                aria-label={t('import_row_description', { description: candidate.description })}
                className="min-w-0 flex-1 basis-56"
                maxLength={MAX_DESCRIPTION}
                disabled={!selectable}
                value={row.description}
                onChange={(event) => {
                  onChange(key, { description: event.target.value });
                }}
              />
              <Select
                aria-label={t('import_row_category', { description: candidate.description })}
                className="w-auto"
                disabled={!selectable}
                value={row.category}
                onChange={(event) => {
                  onChange(key, {
                    category: event.target.value as ExpenseCategory,
                    categoryChosen: true,
                  });
                }}
              >
                {EXPENSE_CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {t(`categories.${c}`)}
                  </option>
                ))}
              </Select>
              <MoneyText amount={candidate.amount} className="w-24 text-end font-semibold" />
              <StatusPill variant={statusVariant[status]}>
                {t(`import_status.${status}`)}
              </StatusPill>
            </div>
            {status === 'PossibleDuplicate' && match && (
              <p className="text-text-2 text-meta mt-1 ps-8">
                {candidate.matches.length > 1
                  ? t('import_duplicate_of_more', {
                      description: match.description ?? t(`categories.${match.category}`),
                      day: dayShort(match.occurredOn),
                      count: candidate.matches.length - 1,
                    })
                  : t('import_duplicate_of', {
                      description: match.description ?? t(`categories.${match.category}`),
                      day: dayShort(match.occurredOn),
                    })}
              </p>
            )}
            {rowErrors.map((message) => (
              <p key={message} role="alert" className="text-destructive text-label mt-1 ps-8">
                {message}
              </p>
            ))}
          </li>
        );
      })}
    </ul>
  );
}
