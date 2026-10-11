import { Banner, Button, Field, Input, PageContainer, PageHeader } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { errorStatus } from '../api/client';
import { checkInErrorDetail, useCheckInMutation, useWalletsQuery } from '../api/queries';
import { formatDate, todayInWarsaw } from '../lib/dates';
import { change, normalizeAmount } from '../lib/money';

import type { Wallet } from '../types';

/** One screen to record this month's balance for any subset of wallets, in one request. */
export default function CheckInPage() {
  const { t, i18n } = useTranslation('investments');
  const { money } = useFormat();
  const toast = useToast();
  const navigate = useNavigate();
  const wallets = useWalletsQuery();
  const mutation = useCheckInMutation();
  const today = todayInWarsaw();
  const [asOf, setAsOf] = useState(today);
  const [amounts, setAmounts] = useState<Record<string, string>>({});

  const active = (wallets.data ?? []).filter((w) => !w.isArchived);
  const rows = active.map((wallet) => {
    const text = amounts[wallet.id]?.trim() ?? '';
    const normalized = text === '' ? null : normalizeAmount(text);
    return { wallet, text, normalized, invalid: text !== '' && normalized === null };
  });
  const filled = rows.filter((r) => r.normalized !== null);
  const anyInvalid = rows.some((r) => r.invalid);
  const dateInvalid = asOf === '' || asOf > today;

  const handleSubmit = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (filled.length === 0 || anyInvalid || dateInvalid) return;
    mutation.mutate(
      {
        asOf,
        entries: filled.map((r) => ({ walletId: r.wallet.id, amount: r.normalized ?? '' })),
      },
      {
        onSuccess: () => {
          toast.success(t('check_in_saved', { count: filled.length }));
          void navigate('/investments');
        },
      },
    );
  };

  const rejection = errorStatus(mutation.error) === 422 ? checkInErrorDetail(mutation.error) : null;

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={t('check_in_title')}
        subtitle={t('check_in_subtitle')}
        breadcrumb={[
          { label: t('net_worth'), href: '/investments' },
          { label: t('check_in_title') },
        ]}
      />
      <form className="space-y-6" noValidate onSubmit={handleSubmit}>
        <Field
          id="check-in-date"
          label={t('date')}
          error={dateInvalid ? t('date_invalid') : undefined}
        >
          <Input
            id="check-in-date"
            type="date"
            max={today}
            value={asOf}
            aria-invalid={dateInvalid}
            className="w-auto"
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setAsOf(e.target.value);
            }}
          />
        </Field>
        {wallets.isPending && <p role="status">{t('loading')}</p>}
        {wallets.isError && <Banner variant="error">{t('load_error')}</Banner>}
        {wallets.isSuccess && active.length === 0 && (
          <p className="text-muted-foreground">{t('check_in_no_wallets')}</p>
        )}
        <ul className="space-y-2">
          {rows.map(({ wallet, normalized, invalid, text }) => (
            <CheckInRow
              key={wallet.id}
              wallet={wallet}
              value={text}
              invalid={invalid}
              delta={
                normalized !== null && wallet.latestBalance !== null
                  ? change(normalized, wallet.latestBalance)
                  : null
              }
              lastLabel={
                wallet.latestBalance !== null && wallet.latestAsOf
                  ? t('last_balance', {
                      amount: money(wallet.latestBalance, { currency: wallet.currency }),
                      date: formatDate(wallet.latestAsOf, i18n.language),
                    })
                  : t('no_balance_yet')
              }
              formatDelta={(d) =>
                `${d.startsWith('-') ? '' : '+'}${money(d, { currency: wallet.currency })}`
              }
              onChange={(value) => {
                setAmounts((current) => ({ ...current, [wallet.id]: value }));
              }}
            />
          ))}
        </ul>
        <p className="text-muted-foreground text-meta">{t('check_in_skip_hint')}</p>
        {mutation.isError && (
          <Banner variant="error">{rejection ?? t('balance_save_error')}</Banner>
        )}
        <div className="flex justify-end">
          <Button
            type="submit"
            disabled={filled.length === 0 || anyInvalid || dateInvalid || mutation.isPending}
          >
            {mutation.isPending ? t('saving') : t('save_balances', { count: filled.length })}
          </Button>
        </div>
      </form>
    </PageContainer>
  );
}

interface CheckInRowProps {
  wallet: Wallet;
  value: string;
  invalid: boolean;
  delta: string | null;
  lastLabel: string;
  formatDelta: (delta: string) => string;
  onChange: (value: string) => void;
}

function CheckInRow({
  wallet,
  value,
  invalid,
  delta,
  lastLabel,
  formatDelta,
  onChange,
}: CheckInRowProps) {
  const { t } = useTranslation('investments');
  const id = `check-in-${wallet.id}`;

  return (
    <li className="border-border grid gap-2 rounded-xl border p-3 sm:grid-cols-[1fr_auto] sm:items-center">
      <div className="min-w-0">
        <label htmlFor={id} className="font-semibold break-words">
          {wallet.name}
        </label>
        <p className="text-muted-foreground text-meta">{lastLabel}</p>
      </div>
      <div className="space-y-1 sm:w-48">
        <Input
          id={id}
          inputMode="decimal"
          autoComplete="off"
          placeholder={wallet.currency}
          value={value}
          aria-invalid={invalid}
          aria-describedby={`${id}-hint`}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
            onChange(e.target.value);
          }}
        />
        <p
          id={`${id}-hint`}
          className={cn(
            'text-meta tnum',
            invalid
              ? 'text-destructive'
              : delta?.startsWith('-')
                ? 'text-destructive'
                : 'text-text-2',
          )}
          aria-live="polite"
        >
          {invalid ? t('amount_invalid_short') : delta === null ? '' : formatDelta(delta)}
        </p>
      </div>
    </li>
  );
}
