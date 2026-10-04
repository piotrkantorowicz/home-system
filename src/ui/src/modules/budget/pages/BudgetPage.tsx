import { useHousehold } from '@modules/household';
import {
  Badge,
  Banner,
  Button,
  Card,
  CardContent,
  LimitMeter,
  MoneyText,
  MonthStepper,
  PageContainer,
  PageHeader,
  SegmentedControl,
  Select,
  Skeleton,
  StatusPill,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { useQueryClient } from '@tanstack/react-query';
import { Plus, Scale, TriangleAlert } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { errorStatus } from '../api/client';
import {
  useBudgetQuery,
  useExpensesQuery,
  useSettlementQuery,
  useSummaryQuery,
} from '../api/queries';
import { budgetQueryKeys } from '../api/queryKeys';
import { ExpenseFormDialog } from '../components/ExpenseFormDialog';
import { ExpenseRow } from '../components/ExpenseRow';
import { LimitDialog } from '../components/LimitDialog';
import { useBudgetAccess } from '../hooks/useBudgetAccess';
import { currentMonth, shiftDays, shiftMonth } from '../lib/dates';
import { limitStatus, sumMinor } from '../lib/limits';
import { minorToDecimal } from '../lib/money';

import type { SummaryScope } from '../api/queries';

export default function BudgetPage() {
  const { t, i18n } = useTranslation('budget');
  const client = useQueryClient();
  const { money } = useFormat();
  const { level } = useBudgetAccess();
  const { household, members, myPersonId } = useHousehold();
  const budget = useBudgetQuery();
  const [month, setMonth] = useState(currentMonth());
  const [scope, setScope] = useState<SummaryScope>(level === 'adult' ? 'shared' : 'personal');
  const [owner, setOwner] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [limitFor, setLimitFor] = useState<string | null>(null);

  const summary = useSummaryQuery(month, scope, scope === 'personal' ? owner : null);
  const personal = scope === 'personal';
  // Settle up is household-wide: never in "Just mine", never for children.
  const showSettle = level === 'adult' && !personal;
  const settlement = useSettlementQuery(showSettle);
  // The list endpoint has no scope filter, so ask for the month and keep only the envelopes this
  // view covers. ponytail: one page of 20; a month with many foreign rows could show fewer than 5.
  const accountIds = new Set(summary.data?.envelopes.map((e) => e.accountId));
  const lastDay = shiftDays(`${shiftMonth(month, 1)}-01`, -1);
  const latest = useExpensesQuery(
    { page: 1, pageSize: 20, from: `${month}-01`, to: lastDay },
    summary.isSuccess,
  );
  const latestRows = (latest.data?.items ?? [])
    .filter((x) => accountIds.has(x.accountId))
    .slice(0, 5);
  const currency = summary.data?.currency ?? budget.data?.currency ?? '';
  const managed = members.filter((m) => m.isManaged);
  const monthLabel = new Intl.DateTimeFormat(i18n.language, {
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${month}-01T00:00:00Z`));
  const reload = () => {
    setLimitFor(null);
    void client.invalidateQueries({ queryKey: budgetQueryKeys.all() });
  };
  const limitEnvelope = summary.data?.envelopes.find((e) => e.accountId === limitFor);
  const envelopeName = (id: string) =>
    summary.data?.envelopes.find((e) => e.accountId === id)?.name ?? '';

  const limited = summary.data?.envelopes.filter((e) => !e.isArchived && e.limit !== null) ?? [];
  const limitedSpent = sumMinor(limited.map((e) => e.spent));
  const limitedTotal = sumMinor(limited.map((e) => e.limit ?? '0'));
  const minor = (n: bigint) => money(minorToDecimal(Number(n)));
  const limitedStatus = limitStatus(
    minorToDecimal(Number(limitedSpent)),
    minorToDecimal(Number(limitedTotal)),
  );
  const categories = [...(summary.data?.categories ?? [])].sort((a, b) => {
    const diff = sumMinor([b.spent]) - sumMinor([a.spent]);
    return diff > 0n ? 1 : diff < 0n ? -1 : 0;
  });
  const topCategory = sumMinor([categories[0]?.spent ?? '0']);
  const transfers =
    showSettle && settlement.data && !settlement.data.isSettled ? settlement.data.suggestions : [];
  const owes = transfers.find((s) => s.fromPersonId === myPersonId);
  const owed = owes ? undefined : transfers.find((s) => s.toPersonId === myPersonId);
  const settleLine = owes
    ? t('settle_owe', { name: owes.toDisplayName, amount: money(owes.amount) })
    : owed
      ? t('settle_owed', { name: owed.fromDisplayName, amount: money(owed.amount) })
      : null;

  return (
    <PageContainer>
      <PageHeader
        title={t('budget_nav')}
        subtitle={
          household?.name
            ? t('overview_subtitle', { household: household.name, currency })
            : t('overview_subtitle_plain', { currency })
        }
        actions={
          <Button
            className="gap-2"
            onClick={() => {
              setAdding(true);
            }}
          >
            <Plus className="size-4" />
            {t('add_expense')}
          </Button>
        }
      />

      <div className="mb-6 flex flex-wrap items-center gap-3">
        <MonthStepper
          label={monthLabel}
          groupLabel={t('month')}
          previousLabel={t('previous_month')}
          nextLabel={t('next_month')}
          onPrevious={() => {
            setMonth(shiftMonth(month, -1));
          }}
          onNext={() => {
            setMonth(shiftMonth(month, 1));
          }}
        />
        {level === 'adult' && (
          <SegmentedControl
            label={t('scope')}
            value={scope}
            onChange={setScope}
            options={[
              { value: 'shared', label: t('scope_shared') },
              { value: 'personal', label: t('scope_mine') },
            ]}
          />
        )}
        {personal && level === 'adult' && managed.length > 0 && (
          <label className="text-sm">
            <span className="text-text-2 mr-2 text-xs font-semibold">{t('whose_spending')}</span>
            <Select
              className="inline-block w-auto"
              value={owner ?? ''}
              onChange={(event) => {
                setOwner(event.target.value || null);
              }}
            >
              <option value="">{t('you')}</option>
              {managed.map((m) => (
                <option key={m.personId} value={m.personId}>
                  {m.displayName}
                </option>
              ))}
            </Select>
          </label>
        )}
      </div>

      {summary.isPending && (
        <div role="status" className="space-y-3">
          <span className="sr-only">{t('loading')}</span>
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}
      {summary.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void summary.refetch();
          }}
          retryLabel={t('retry')}
        >
          {errorStatus(summary.error) === 403 ? t('unavailable_body') : t('load_error')}
        </Banner>
      )}

      {summary.isSuccess && (
        <div className="space-y-6">
          <Card>
            <CardContent className="space-y-4 pt-6">
              <div>
                <p className="text-muted-foreground text-label font-semibold uppercase">
                  {t('spent_this_month')}
                </p>
                <MoneyText
                  amount={summary.data.totalSpent}
                  className="text-display block font-bold"
                />
                <p className="text-text-2 text-meta">
                  {t('across_envelopes', { count: summary.data.envelopes.length })} ·{' '}
                  {t('expense_count', { count: Number(summary.data.expenseCount) })}
                </p>
              </div>
              {limitedStatus && (
                <div className="space-y-1">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <p className="text-body">
                      {t('limits_summary')}:{' '}
                      {t('of_limit', { spent: minor(limitedSpent), limit: minor(limitedTotal) })}
                    </p>
                    <StatusPill variant={limitedStatus.isOver ? 'over' : 'good'}>
                      {limitedStatus.isOver
                        ? t('over_short', { amount: minor(limitedStatus.over) })
                        : t('left_short', { amount: minor(limitedStatus.left) })}
                    </StatusPill>
                  </div>
                  <LimitMeter
                    percent={limitedStatus.percent}
                    over={limitedStatus.isOver}
                    limitAt={limitedStatus.limitAt}
                  />
                </div>
              )}
              {settleLine && (
                <Link
                  to="/budget/settlement"
                  className="border-border hover:bg-accent focus-visible:ring-ring flex items-center justify-between gap-3 rounded-lg border p-3 focus-visible:ring-2 focus-visible:outline-none"
                >
                  <span className="flex items-center gap-2 font-semibold">
                    <Scale className="size-4" aria-hidden="true" />
                    {settleLine}
                  </span>
                  <span className="text-primary text-sm">{t('record_payment_link')} ›</span>
                </Link>
              )}
            </CardContent>
          </Card>

          <section aria-labelledby="envelopes-title" className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h2 id="envelopes-title" className="text-section font-semibold">
                {t('envelopes')}
              </h2>
              <Link to="/budget/envelopes" className="text-primary text-sm underline">
                {t('manage_envelopes')}
              </Link>
            </div>
            {summary.data.envelopes.length === 0 ? (
              <p className="text-text-2 text-sm">{t('no_envelopes_in_scope')}</p>
            ) : (
              <ul className="space-y-3">
                {summary.data.envelopes.map((e) => {
                  const status = e.limit === null ? null : limitStatus(e.spent, e.limit);
                  return (
                    <li key={e.accountId} className="border-border space-y-2 rounded-xl border p-4">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="font-semibold break-words">
                            {e.name}
                            {e.isArchived && (
                              <Badge variant="outline" className="ml-2">
                                {t('archived')}
                              </Badge>
                            )}
                          </p>
                          <p className="text-text-2 text-sm">
                            {e.limit === null ? (
                              <>
                                <MoneyText amount={e.spent} /> · {t('no_limit')}
                              </>
                            ) : (
                              t('of_limit', { spent: money(e.spent), limit: money(e.limit) })
                            )}
                          </p>
                        </div>
                        {!e.isArchived && (
                          <Button
                            variant="ghost"
                            size="sm"
                            aria-label={t(
                              e.limit === null ? 'set_limit_named' : 'change_limit_named',
                              { name: e.name },
                            )}
                            onClick={() => {
                              setLimitFor(e.accountId);
                            }}
                          >
                            {t(e.limit === null ? 'set_limit' : 'change_limit')}
                          </Button>
                        )}
                      </div>
                      {status && (
                        <>
                          <LimitMeter
                            percent={status.percent}
                            over={status.isOver}
                            limitAt={status.limitAt}
                          />
                          <p
                            className={
                              status.isOver
                                ? 'text-destructive flex items-center gap-1 text-sm font-semibold'
                                : 'text-text-2 text-sm'
                            }
                          >
                            {status.isOver ? (
                              <>
                                <TriangleAlert className="size-4" aria-hidden="true" />
                                {t('over_short', { amount: minor(status.over) })}
                              </>
                            ) : (
                              t('left_short', { amount: minor(status.left) })
                            )}
                          </p>
                        </>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          {summary.data.categories.length > 0 && (
            <section aria-labelledby="categories-title" className="space-y-2">
              <h2 id="categories-title" className="text-section font-semibold">
                {t('by_category')}
              </h2>
              <ul className="space-y-2 text-sm">
                {categories.map((c) => (
                  <li key={c.category} className="flex items-center gap-3">
                    <span className="w-28 shrink-0">{t(`categories.${c.category}`)}</span>
                    <span className="min-w-0 flex-1">
                      <span aria-hidden="true" className="bg-muted block h-2 rounded-full">
                        <span
                          className="bg-primary block h-full rounded-full"
                          style={{
                            width: `${String(topCategory === 0n ? 0 : Number((sumMinor([c.spent]) * 100n) / topCategory))}%`,
                          }}
                        />
                      </span>
                    </span>
                    <MoneyText amount={c.spent} className="w-24 shrink-0 text-right" />
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section aria-labelledby="recent-title" className="space-y-2">
            <div className="flex items-center justify-between gap-3">
              <h2 id="recent-title" className="text-section font-semibold">
                {t('latest')}
              </h2>
              <Link to="/budget/expenses" className="text-primary text-sm underline">
                {t('all_expenses')}
              </Link>
            </div>
            {latest.isPending && <Skeleton className="h-16 w-full" />}
            {latest.isError && (
              <Banner
                variant="error"
                onRetry={() => {
                  void latest.refetch();
                }}
                retryLabel={t('retry')}
              >
                {t('load_error')}
              </Banner>
            )}
            {latest.isSuccess && latestRows.length === 0 && (
              <p className="text-text-2 text-sm">{t('no_expenses_body')}</p>
            )}
            <ul>
              {latestRows.map((x) => (
                <li key={x.id}>
                  <ExpenseRow
                    expense={x}
                    envelopeName={envelopeName(x.accountId)}
                    myPersonId={myPersonId}
                  />
                </li>
              ))}
            </ul>
          </section>
        </div>
      )}

      {adding && (
        <ExpenseFormDialog
          onClose={() => {
            setAdding(false);
          }}
        />
      )}
      {limitEnvelope && (
        <LimitDialog
          accountId={limitEnvelope.accountId}
          accountName={limitEnvelope.name}
          month={month}
          currency={currency}
          limit={
            limitEnvelope.limit === null || limitEnvelope.limitRevision === null
              ? null
              : { amount: limitEnvelope.limit, revision: Number(limitEnvelope.limitRevision) }
          }
          onClose={() => {
            setLimitFor(null);
          }}
          onReload={reload}
        />
      )}
    </PageContainer>
  );
}
