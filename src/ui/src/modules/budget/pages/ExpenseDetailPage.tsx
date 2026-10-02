import { Badge, Banner, Button } from '@shared/components/ui';
import { useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';

import { errorStatus } from '../api/client';
import { useAccountsQuery, useBudgetQuery, useExpenseQuery } from '../api/queries';
import { budgetQueryKeys } from '../api/queryKeys';
import { ExpenseFormDialog } from '../components/ExpenseFormDialog';
import { VoidExpenseDialog } from '../components/VoidExpenseDialog';

export default function ExpenseDetailPage() {
  const { t } = useTranslation('budget');
  const { id = '' } = useParams();
  const client = useQueryClient();
  const budget = useBudgetQuery();
  const accounts = useAccountsQuery();
  const query = useExpenseQuery(id);
  const [dialog, setDialog] = useState<'correct' | 'void' | null>(null);
  const currency = budget.data?.currency ?? '';
  const reload = () => {
    setDialog(null);
    void client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
  };

  if (query.isPending) return <p role="status">{t('loading')}</p>;
  if (query.isError)
    return errorStatus(query.error) === 404 ? (
      <Banner variant="warning">{t('expense_not_found')}</Banner>
    ) : (
      <Banner
        variant="error"
        onRetry={() => {
          void query.refetch();
        }}
        retryLabel={t('retry')}
      >
        {t('load_error')}
      </Banner>
    );

  const expense = query.data;
  const accountName = accounts.data?.find((a) => a.id === expense.accountId)?.name ?? '';
  return (
    <div className="space-y-6">
      <Link
        to="/budget/expenses"
        className="text-primary inline-flex items-center gap-1 text-sm underline"
      >
        <ArrowLeft className="size-4" />
        {t('back_to_expenses')}
      </Link>

      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold break-words">
            {expense.amount} {currency} · {t(`categories.${expense.category}`)}
          </h2>
          <p className="text-text-2 mt-1 text-sm">
            {expense.occurredOn} · {accountName}
          </p>
          {expense.isVoided && (
            <Badge variant="destructive" className="mt-2">
              {t('voided')}
            </Badge>
          )}
        </div>
        {!expense.isVoided && (
          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setDialog('correct');
              }}
            >
              {t('correct')}
            </Button>
            <Button
              variant="ghost"
              onClick={() => {
                setDialog('void');
              }}
            >
              {t('void')}
            </Button>
          </div>
        )}
      </header>

      <dl className="grid gap-4 sm:grid-cols-2">
        <div>
          <dt className="text-text-2 text-xs font-semibold">{t('recorded_by_label')}</dt>
          <dd>{expense.addedByDisplayName}</dd>
        </div>
        <div>
          <dt className="text-text-2 text-xs font-semibold">{t('paid_by')}</dt>
          <dd>{expense.paidByDisplayName ?? t('paid_from_household')}</dd>
        </div>
      </dl>

      {expense.shares.length > 0 && (
        <section aria-labelledby="shares-title">
          <h3 id="shares-title" className="mb-2 font-semibold">
            {t('shares')}
          </h3>
          <ul className="space-y-1 text-sm">
            {expense.shares.map((s) => (
              <li key={s.personId}>
                {s.personDisplayName} · {s.amount} {currency}
              </li>
            ))}
          </ul>
        </section>
      )}

      <section aria-labelledby="history-title">
        <h3 id="history-title" className="mb-2 font-semibold">
          {t('history')}
        </h3>
        <ol className="space-y-3">
          {(expense.history ?? []).map((r) => (
            <li
              key={String(r.revisionNumber)}
              className="border-border rounded-xl border p-3 text-sm"
            >
              <p className="font-semibold">
                {t('revision', { number: Number(r.revisionNumber) })} ·{' '}
                {t(`operations.${r.operation}`)}
              </p>
              <p className="text-text-2">
                {t('changed_by', { name: r.actorDisplayName })} ·{' '}
                {r.createdAt.slice(0, 16).replace('T', ' ')} UTC
              </p>
              {r.reason && <p>{t('reason_is', { reason: r.reason })}</p>}
              <p>
                {r.snapshot.amount} {currency} · {t(`categories.${r.snapshot.category}`)} ·{' '}
                {r.snapshot.occurredOn}
                {r.snapshot.isVoided ? ` · ${t('voided')}` : ''}
              </p>
              {r.snapshot.shares.length > 0 && (
                <ul className="text-text-2">
                  {r.snapshot.shares.map((s) => (
                    <li key={s.personId}>
                      {s.personDisplayName} · {s.amount} {currency}
                    </li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ol>
      </section>

      {dialog === 'correct' && (
        <ExpenseFormDialog
          expense={expense}
          onClose={() => {
            setDialog(null);
          }}
          onReload={reload}
        />
      )}
      {dialog === 'void' && (
        <VoidExpenseDialog
          expense={expense}
          onClose={() => {
            setDialog(null);
          }}
          onReload={reload}
        />
      )}
    </div>
  );
}
