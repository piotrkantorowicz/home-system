import { useHousehold } from '@modules/household';
import { Badge, Banner, Button, MoneyText, PageContainer, PageHeader } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';

import { errorStatus } from '../api/client';
import { useAccountsQuery, useBudgetQuery, useExpenseQuery } from '../api/queries';
import { budgetQueryKeys } from '../api/queryKeys';
import { ExpenseFormDialog } from '../components/ExpenseFormDialog';
import { ExpenseHistory } from '../components/ExpenseHistory';
import { VoidExpenseDialog } from '../components/VoidExpenseDialog';
import { useBudgetAccess } from '../hooks/useBudgetAccess';
import { toMinor } from '../lib/money';

export default function ExpenseDetailPage() {
  const { t } = useTranslation('budget');
  const { id = '' } = useParams();
  const client = useQueryClient();
  const { level } = useBudgetAccess();
  const { myPersonId } = useHousehold();
  const { money: formatMoney } = useFormat();
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
  const account = accounts.data?.find((a) => a.id === expense.accountId);
  const category = t(`categories.${expense.category}`);
  const title = expense.description?.trim() ? expense.description.trim() : category;
  const canChange =
    !expense.isVoided && (level === 'adult' || expense.addedByPersonId === myPersonId);
  const payer = expense.paidByDisplayName;
  const owed = expense.shares.find((s) => s.personId === myPersonId);
  const totalMinor = toMinor(expense.amount);
  const money = (amount: string) => formatMoney(amount, { currency });
  const sentence = !payer
    ? t('paid_from_household')
    : expense.paidByPersonId === myPersonId
      ? t('paid_sentence', { name: t('you') })
      : owed
        ? `${t('paid_sentence', { name: payer })} ${t('owe_sentence', { name: payer, amount: money(owed.amount) })}`
        : t('paid_sentence', { name: payer });

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={title}
        breadcrumb={[{ label: t('expenses'), href: '/budget/expenses' }, { label: title }]}
        subtitle={[category, account?.name, expense.occurredOn].filter(Boolean).join(' · ')}
        actions={
          canChange ? (
            <Button
              variant="outline"
              onClick={() => {
                setDialog('correct');
              }}
            >
              {t('correct')}
            </Button>
          ) : undefined
        }
      />
      <div className="space-y-8">
        <div className="flex flex-wrap items-center gap-3">
          <MoneyText
            amount={expense.amount}
            currency={currency}
            className={`text-[40px] leading-tight font-bold ${expense.isVoided ? 'line-through' : ''}`}
          />
          {expense.isVoided && <Badge variant="destructive">{t('voided')}</Badge>}
        </div>

        <section aria-labelledby="who-title" className="space-y-3">
          <h2 id="who-title" className="font-semibold">
            {t('who_pays')}
          </h2>
          <p className="text-sm">{sentence}</p>
          {expense.shares.length > 0 && totalMinor > 0 && (
            <>
              <div aria-hidden="true" className="bg-muted flex h-3 overflow-hidden rounded-full">
                {expense.shares.map((s, i) => (
                  <span
                    key={s.personId}
                    className="bg-primary h-full border-r border-white last:border-r-0 dark:border-black"
                    style={{
                      width: `${String((toMinor(s.amount) / totalMinor) * 100)}%`,
                      opacity: 1 - (i % 4) * 0.22,
                    }}
                  />
                ))}
              </div>
              <ul className="space-y-1 text-sm">
                {expense.shares.map((s) => (
                  <li key={s.personId} className="flex justify-between gap-3">
                    <span>{s.personDisplayName}</span>
                    <MoneyText amount={s.amount} currency={currency} />
                  </li>
                ))}
              </ul>
            </>
          )}
        </section>

        <section aria-labelledby="details-title">
          <h2 id="details-title" className="mb-2 font-semibold">
            {t('details')}
          </h2>
          <dl className="grid gap-4 sm:grid-cols-2">
            {[
              [t('detail_envelope'), account?.name ?? ''],
              [t('detail_category'), category],
              [t('detail_date'), expense.occurredOn],
              [t('recorded_by_label'), expense.addedByDisplayName],
            ].map(([label, value]) => (
              <div key={label}>
                <dt className="text-text-2 text-xs font-semibold">{label}</dt>
                <dd>{value}</dd>
              </div>
            ))}
          </dl>
        </section>

        <section aria-labelledby="history-title">
          <h2 id="history-title" className="mb-2 font-semibold">
            {t('history')}
          </h2>
          <ExpenseHistory
            history={expense.history ?? []}
            currency={currency}
            myPersonId={myPersonId}
          />
        </section>

        {canChange && (
          <section
            aria-labelledby="void-title"
            className="border-destructive/50 flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4"
          >
            <div className="min-w-0">
              <h2 id="void-title" className="font-semibold">
                {t('void_row_title')}
              </h2>
              <p className="text-text-2 text-sm">{t('void_description')}</p>
            </div>
            <Button
              variant="outline"
              onClick={() => {
                setDialog('void');
              }}
            >
              {t('void')}
            </Button>
          </section>
        )}
      </div>

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
    </PageContainer>
  );
}
