import {
  Badge,
  Banner,
  Button,
  EmptyState,
  Field,
  Pagination,
  Select,
  Input,
  Checkbox,
} from '@shared/components/ui';
import { Plus, Receipt } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { useAccountsQuery, useBudgetQuery, useExpensesQuery } from '../api/queries';
import { ExpenseFormDialog } from '../components/ExpenseFormDialog';
import { isIsoDate } from '../lib/dates';
import { EXPENSE_CATEGORIES } from '../types';

import type { ExpenseCategory, ExpenseFilters } from '../types';

export default function ExpensesPage() {
  const { t } = useTranslation('budget');
  const budget = useBudgetQuery();
  const accounts = useAccountsQuery();
  // `?add=1` opens the form: the phone tab bar's raised "Add expense" button links here.
  const [searchParams, setSearchParams] = useSearchParams();
  const adding = searchParams.get('add') === '1';
  const setAdding = (open: boolean) => {
    setSearchParams(open ? { add: '1' } : {}, { replace: true });
  };
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [category, setCategory] = useState('');
  const [accountId, setAccountId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [includeVoided, setIncludeVoided] = useState(false);

  const filters: ExpenseFilters = {
    page,
    pageSize,
    ...(category ? { category: category as ExpenseCategory } : {}),
    ...(accountId ? { accountId } : {}),
    ...(isIsoDate(from) ? { from } : {}),
    ...(isIsoDate(to) ? { to } : {}),
    ...(includeVoided ? { includeVoided: true } : {}),
  };
  const query = useExpensesQuery(filters);
  const currency = budget.data?.currency ?? '';
  const accountName = (id: string) => accounts.data?.find((a) => a.id === id)?.name ?? '';
  const filtered = category !== '' || accountId !== '' || from !== '' || to !== '' || includeVoided;
  const reset = (apply: () => void) => {
    apply();
    setPage(1);
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="max-w-xl">
          <h2 className="text-xl font-semibold">{t('expenses')}</h2>
          <p className="text-text-2 mt-1 text-sm">{t('expenses_intro')}</p>
        </div>
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

      <form
        className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4"
        aria-label={t('filters')}
        onSubmit={(event) => {
          event.preventDefault();
        }}
      >
        <Field id="filter-category" label={t('category')}>
          <Select
            id="filter-category"
            value={category}
            onChange={(event) => {
              reset(() => {
                setCategory(event.target.value);
              });
            }}
          >
            <option value="">{t('all')}</option>
            {EXPENSE_CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {t(`categories.${c}`)}
              </option>
            ))}
          </Select>
        </Field>
        <Field id="filter-envelope" label={t('envelope')}>
          <Select
            id="filter-envelope"
            value={accountId}
            onChange={(event) => {
              reset(() => {
                setAccountId(event.target.value);
              });
            }}
          >
            <option value="">{t('all')}</option>
            {accounts.data?.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </Select>
        </Field>
        <Field id="filter-from" label={t('from')}>
          <Input
            id="filter-from"
            type="date"
            value={from}
            onChange={(event) => {
              reset(() => {
                setFrom(event.target.value);
              });
            }}
          />
        </Field>
        <Field id="filter-to" label={t('to')}>
          <Input
            id="filter-to"
            type="date"
            value={to}
            onChange={(event) => {
              reset(() => {
                setTo(event.target.value);
              });
            }}
          />
        </Field>
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
      </form>

      {query.isPending && <p role="status">{t('loading')}</p>}
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
          <ul className="space-y-3">
            {query.data.items.map((e) => (
              <li key={e.id}>
                <Link
                  to={`/budget/expenses/${e.id}`}
                  className="border-border hover:bg-accent focus-visible:ring-ring flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4 focus-visible:ring-2 focus-visible:outline-none"
                >
                  <div className="min-w-0">
                    <p className="font-semibold break-words">
                      {t(`categories.${e.category}`)} · {accountName(e.accountId)}
                    </p>
                    <p className="text-text-2 text-sm">
                      {e.occurredOn} · {t('recorded_by', { name: e.addedByDisplayName })} ·{' '}
                      {e.paidByDisplayName
                        ? t('paid_by_name', { name: e.paidByDisplayName })
                        : t('paid_from_household')}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    {e.isVoided && <Badge variant="destructive">{t('voided')}</Badge>}
                    <span className="font-semibold">
                      {e.amount} {currency}
                    </span>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
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
    </div>
  );
}
