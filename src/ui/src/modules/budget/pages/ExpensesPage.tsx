import { useHousehold } from '@modules/household';
import {
  Banner,
  Button,
  Checkbox,
  EmptyState,
  Input,
  MoneyText,
  MonthStepper,
  PageContainer,
  PageHeader,
  Pagination,
  Select,
  Skeleton,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { Plus, Receipt } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';

import { useAccountsQuery, useBudgetQuery, useExpensesQuery } from '../api/queries';
import { ExpenseFormDialog } from '../components/ExpenseFormDialog';
import { ExpenseRow } from '../components/ExpenseRow';
import {
  currentMonth,
  formatMonth,
  monthEnd,
  shiftDays,
  shiftMonth,
  todayLocal,
} from '../lib/dates';
import { EXPENSE_CATEGORIES } from '../types';

import type { Expense, ExpenseCategory, ExpenseFilters } from '../types';

export default function ExpensesPage() {
  const { t, i18n } = useTranslation('budget');
  const { dayShort } = useFormat();
  const { myPersonId } = useHousehold();
  const budget = useBudgetQuery();
  const accounts = useAccountsQuery();
  // `?add=1` opens the form: the phone tab bar's raised "Add expense" button links here.
  const [searchParams, setSearchParams] = useSearchParams();
  const adding = searchParams.get('add') === '1';
  const setAdding = (open: boolean) => {
    setSearchParams(open ? { add: '1' } : {}, { replace: true });
  };
  const [month, setMonth] = useState(currentMonth());
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [category, setCategory] = useState('');
  const [accountId, setAccountId] = useState('');
  const [includeVoided, setIncludeVoided] = useState(false);
  const [searchText, setSearchText] = useState('');
  const [search, setSearch] = useState('');

  // Typing is applied to the query after a short pause so each keystroke is not a request.
  useEffect(() => {
    const id = setTimeout(() => {
      const next = searchText.trim();
      if (next !== search) {
        setSearch(next);
        setPage(1);
      }
    }, 300);
    return () => {
      clearTimeout(id);
    };
  }, [searchText, search]);

  const filters: ExpenseFilters = {
    page,
    pageSize,
    from: `${month}-01`,
    to: monthEnd(month),
    ...(category ? { category: category as ExpenseCategory } : {}),
    ...(accountId ? { accountId } : {}),
    ...(search ? { search } : {}),
    ...(includeVoided ? { includeVoided: true } : {}),
  };
  const query = useExpensesQuery(filters);
  const currency = budget.data?.currency ?? '';
  const accountName = (id: string) => accounts.data?.find((a) => a.id === id)?.name ?? '';
  const filtered = category !== '' || accountId !== '' || search !== '' || includeVoided;
  const reset = (apply: () => void) => {
    apply();
    setPage(1);
  };

  const today = todayLocal();
  const dayLabel = (date: string) => {
    if (date === today) return t('day_today', { day: dayShort(date) });
    if (date === shiftDays(today, -1)) return t('day_yesterday', { day: dayShort(date) });
    return dayShort(date);
  };
  // Server totals cover the whole day across every page, so a day split by paging stays correct.
  const dayTotals = new Map(query.data?.dailyTotals.map((d) => [d.date, d.total]));
  const groups: { date: string; items: Expense[] }[] = [];
  for (const e of query.data?.items ?? []) {
    const last = groups.at(-1);
    if (last?.date === e.occurredOn) last.items.push(e);
    else groups.push({ date: e.occurredOn, items: [e] });
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('expenses')}
        subtitle={currency ? t('overview_subtitle_plain', { currency }) : undefined}
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

      <form
        className="mb-4 flex flex-wrap items-center gap-3"
        aria-label={t('filters')}
        onSubmit={(event) => {
          event.preventDefault();
        }}
      >
        <MonthStepper
          label={formatMonth(month, i18n.language)}
          groupLabel={t('month')}
          previousLabel={t('previous_month')}
          nextLabel={t('next_month')}
          onPrevious={() => {
            reset(() => {
              setMonth(shiftMonth(month, -1));
            });
          }}
          onNext={() => {
            reset(() => {
              setMonth(shiftMonth(month, 1));
            });
          }}
        />
        <Input
          type="search"
          aria-label={t('search_expenses')}
          placeholder={t('search_expenses')}
          className="w-full sm:w-56"
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value);
          }}
        />
        <Select
          aria-label={t('envelope')}
          className="w-auto"
          value={accountId}
          onChange={(event) => {
            reset(() => {
              setAccountId(event.target.value);
            });
          }}
        >
          <option value="">{t('all_envelopes')}</option>
          {accounts.data?.map((a) => (
            <option key={a.id} value={a.id}>
              {a.name}
            </option>
          ))}
        </Select>
        <Select
          aria-label={t('category')}
          className="w-auto"
          value={category}
          onChange={(event) => {
            reset(() => {
              setCategory(event.target.value);
            });
          }}
        >
          <option value="">{t('all_categories')}</option>
          {EXPENSE_CATEGORIES.map((c) => (
            <option key={c} value={c}>
              {t(`categories.${c}`)}
            </option>
          ))}
        </Select>
      </form>

      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <p className="text-text-2 text-meta">
          {query.isSuccess && (
            <>
              {t('expense_count', { count: Number(query.data.activeCount) })} ·{' '}
              <MoneyText amount={query.data.totalAmount} /> {t('totals_total')} ·{' '}
              {t('totals_share')} <MoneyText amount={query.data.yourShareAmount} />
            </>
          )}
        </p>
        <label className="flex items-center gap-2 text-sm">
          <Checkbox
            checked={includeVoided}
            onChange={(event) => {
              reset(() => {
                setIncludeVoided(event.target.checked);
              });
            }}
          />
          {t('show_voided')}
        </label>
      </div>

      {query.isPending && (
        <div role="status" className="space-y-2">
          <span className="sr-only">{t('loading')}</span>
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      )}
      {query.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void query.refetch();
          }}
          retryLabel={t('retry')}
        >
          {t('load_error')}
        </Banner>
      )}
      {query.isSuccess && query.data.items.length === 0 && (
        <EmptyState
          icon={Receipt}
          title={t(filtered ? 'no_expenses_filtered_title' : 'no_expenses_title')}
          description={t(filtered ? 'no_expenses_filtered_body' : 'no_expenses_body')}
        />
      )}
      {query.isSuccess && query.data.items.length > 0 && (
        <>
          <div className="space-y-4">
            {groups.map((g) => (
              <section key={g.date} aria-label={dayLabel(g.date)}>
                <div className="text-text-2 text-meta flex items-center justify-between gap-3 px-2 font-semibold">
                  <h2 className="text-meta font-semibold">{dayLabel(g.date)}</h2>
                  {dayTotals.has(g.date) && <MoneyText amount={dayTotals.get(g.date)} />}
                </div>
                <ul>
                  {g.items.map((e) => (
                    <li key={e.id}>
                      <ExpenseRow
                        expense={e}
                        envelopeName={accountName(e.accountId)}
                        myPersonId={myPersonId}
                      />
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
          <Pagination
            page={Number(query.data.page)}
            pageSize={Number(query.data.pageSize)}
            totalCount={Number(query.data.totalCount)}
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        </>
      )}

      {adding && (
        <ExpenseFormDialog
          onClose={() => {
            setAdding(false);
          }}
        />
      )}
    </PageContainer>
  );
}
