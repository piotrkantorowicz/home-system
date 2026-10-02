import { useHousehold } from '@modules/household';
import {
  Badge,
  Banner,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Select,
} from '@shared/components/ui';
import { useQueryClient } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight, Plus, TriangleAlert } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { errorStatus } from '../api/client';
import { useBudgetQuery, useExpensesQuery, useSummaryQuery } from '../api/queries';
import { budgetQueryKeys } from '../api/queryKeys';
import { ExpenseFormDialog } from '../components/ExpenseFormDialog';
import { LimitDialog } from '../components/LimitDialog';
import { useBudgetAccess } from '../hooks/useBudgetAccess';
import { currentMonth, shiftMonth } from '../lib/dates';

import type { SummaryScope } from '../api/queries';

export default function BudgetPage() {
  const { t, i18n } = useTranslation('budget');
  const client = useQueryClient();
  const { level } = useBudgetAccess();
  const { members } = useHousehold();
  const budget = useBudgetQuery();
  const [month, setMonth] = useState(currentMonth());
  const [scope, setScope] = useState<SummaryScope>(level === 'adult' ? 'shared' : 'personal');
  const [owner, setOwner] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [limitFor, setLimitFor] = useState<string | null>(null);

  const summary = useSummaryQuery(month, scope, scope === 'personal' ? owner : null);
  const recent = useExpensesQuery({ page: 1, pageSize: 5 });
  const currency = summary.data?.currency ?? budget.data?.currency ?? '';
  const managed = members.filter((m) => m.isManaged);
  const personal = scope === 'personal';
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

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <p className="text-text-2 max-w-xl text-sm">{t('overview_intro')}</p>
        <Button
          className="gap-2"
          onClick={() => {
            setAdding(true);
          }}
        >
          <Plus className="size-4" />
          {t('add_expense')}
        </Button>
      </header>

      <div className="flex flex-wrap items-end gap-3">
        <div role="group" aria-label={t('month')} className="flex items-center gap-1">
          <Button
            variant="outline"
            size="icon"
            aria-label={t('previous_month')}
            onClick={() => {
              setMonth(shiftMonth(month, -1));
            }}
          >
            <ChevronLeft className="size-4" />
          </Button>
          <p className="min-w-40 text-center font-semibold capitalize" aria-live="polite">
            {monthLabel}
          </p>
          <Button
            variant="outline"
            size="icon"
            aria-label={t('next_month')}
            onClick={() => {
              setMonth(shiftMonth(month, 1));
            }}
          >
            <ChevronRight className="size-4" />
          </Button>
        </div>
        {level === 'adult' && (
          <div role="group" aria-label={t('scope')} className="flex gap-1">
            <Button
              variant={scope === 'shared' ? 'secondary' : 'ghost'}
              size="sm"
              aria-pressed={scope === 'shared'}
              onClick={() => {
                setScope('shared');
              }}
            >
              {t('scope_shared')}
            </Button>
            <Button
              variant={personal ? 'secondary' : 'ghost'}
              size="sm"
              aria-pressed={personal}
              onClick={() => {
                setScope('personal');
              }}
            >
              {t('scope_mine')}
            </Button>
          </div>
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

      {summary.isPending && <p role="status">{t('loading')}</p>}
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
        <>
          <Card>
            <CardHeader>
              <CardTitle>
                {t(personal ? 'total_mine' : 'total_shared', { month: monthLabel })}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold">
                {summary.data.totalSpent} {currency}
              </p>
              <p className="text-text-2 mt-1 text-sm">{t('spending_note')}</p>
            </CardContent>
          </Card>

          <section aria-labelledby="envelopes-title" className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <h2 id="envelopes-title" className="text-lg font-semibold">
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
                  const remaining = e.remaining;
                  return (
                    <li
                      key={e.accountId}
                      className="border-border flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4"
                    >
                      <div className="min-w-0">
                        <p className="font-semibold break-words">
                          {e.name}
                          {e.isArchived && (
                            <Badge variant="outline" className="ml-2">
                              {t('archived')}
                            </Badge>
                          )}
                        </p>
                        <p className="text-sm">
                          {t('spent_of', { spent: e.spent, currency })}
                          {e.limit === null
                            ? ` · ${t('no_limit')}`
                            : ` · ${t('limit_is', { limit: e.limit, currency })}`}
                        </p>
                        {remaining !== null && (
                          <p
                            className={
                              e.isOverspent
                                ? 'text-destructive flex items-center gap-1 text-sm font-semibold'
                                : 'text-text-2 text-sm'
                            }
                          >
                            {e.isOverspent ? (
                              <>
                                <TriangleAlert className="size-4" aria-hidden="true" />
                                {t('over_by', { amount: remaining.replace('-', ''), currency })}
                              </>
                            ) : (
                              t('left', { amount: remaining, currency })
                            )}
                          </p>
                        )}
                      </div>
                      {!e.isArchived && (
                        <Button
                          variant="outline"
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
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          {summary.data.categories.length > 0 && (
            <section aria-labelledby="categories-title" className="space-y-2">
              <h2 id="categories-title" className="text-lg font-semibold">
                {t('by_category')}
              </h2>
              <ul className="space-y-1 text-sm">
                {summary.data.categories.map((c) => (
                  <li key={c.category} className="flex justify-between gap-3">
                    <span>{t(`categories.${c.category}`)}</span>
                    <span>
                      {c.spent} {currency}
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </>
      )}

      <section aria-labelledby="recent-title" className="space-y-2">
        <div className="flex items-center justify-between gap-3">
          <h2 id="recent-title" className="text-lg font-semibold">
            {t('recent_expenses')}
          </h2>
          <Link to="/budget/expenses" className="text-primary text-sm underline">
            {t('view_expenses')}
          </Link>
        </div>
        {recent.isSuccess && recent.data.items.length === 0 && (
          <p className="text-text-2 text-sm">{t('no_expenses_body')}</p>
        )}
        <ul className="space-y-1 text-sm">
          {recent.data?.items.map((x) => (
            <li key={x.id}>
              <Link to={`/budget/expenses/${x.id}`} className="underline">
                {x.occurredOn} · {t(`categories.${x.category}`)} · {x.amount} {currency}
              </Link>
            </li>
          ))}
        </ul>
      </section>

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
    </div>
  );
}
