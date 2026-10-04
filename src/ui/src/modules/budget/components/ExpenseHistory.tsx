import { MoneyText } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

import { buildTimeline, type FieldChange } from '../lib/history';

import type { Expense } from '../types';

export interface ExpenseHistoryProps {
  history: NonNullable<Expense['history']>;
  currency: string;
  myPersonId: string | null;
}

function Money({ amount, currency }: { amount: string; currency: string }) {
  return <MoneyText amount={amount} currency={currency} />;
}

function ChangeLine({ change, currency }: { change: FieldChange; currency: string }) {
  const { t } = useTranslation('budget');
  if (change.field === 'voided') return <>{t('field_voided')}</>;
  const label =
    change.field === 'share' ? t('field_share', { name: change.name }) : t(`field_${change.field}`);
  const money =
    change.field === 'amount' || change.field === 'each_share' || change.field === 'share';
  const view = (value: string) => {
    if (money) return <Money amount={value} currency={currency} />;
    if (!value) return t('history_empty_value');
    return change.field === 'category' ? t(`categories.${value}`) : value;
  };
  return (
    <>
      {label} <del className="text-muted-foreground">{view(change.from)}</del> →{' '}
      <strong>{view(change.to)}</strong>
    </>
  );
}

/** The revision timeline: newest first, with only the fields each revision changed. */
export function ExpenseHistory({ history, currency, myPersonId }: ExpenseHistoryProps) {
  const { t, i18n } = useTranslation('budget');
  const when = (iso: string) =>
    new Intl.DateTimeFormat(i18n.language, {
      day: 'numeric',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(iso));

  return (
    <ol className="space-y-3">
      {buildTimeline(history).map(({ revision: r, changes }) => {
        const name = r.actorPersonId === myPersonId ? t('you').toLowerCase() : r.actorDisplayName;
        const title =
          changes === null
            ? 'history_added'
            : r.operation === 'Void'
              ? 'history_voided'
              : 'history_corrected';
        return (
          <li
            key={String(r.revisionNumber)}
            className="border-border rounded-xl border p-3 text-sm"
          >
            <p className="font-semibold">{t(title, { name, when: when(r.createdAt) })}</p>
            {r.reason && <p className="text-text-2">“{r.reason}”</p>}
            {changes === null ? (
              <p>
                {r.snapshot.description?.trim()
                  ? r.snapshot.description.trim()
                  : t(`categories.${r.snapshot.category}`)}{' '}
                · <Money amount={r.snapshot.amount} currency={currency} />
              </p>
            ) : changes.length === 0 ? (
              <p className="text-text-2">{t('history_no_changes')}</p>
            ) : (
              <ul>
                {changes.map((c, i) => (
                  <li key={`${c.field}-${String(i)}`}>
                    <ChangeLine change={c} currency={currency} />
                  </li>
                ))}
              </ul>
            )}
          </li>
        );
      })}
    </ol>
  );
}
