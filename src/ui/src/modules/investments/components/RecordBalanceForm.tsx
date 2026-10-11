import { Banner, Button, Field, Input } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useSnapshotMutation } from '../api/queries';
import { todayLocal } from '../lib/dates';
import { normalizeAmount } from '../lib/money';

import type { Wallet } from '../types';

const NOTE_MAX = 200;

export interface RecordBalanceFormProps {
  wallet: Wallet;
}

/** Record a wallet's balance for a date; the same date replaces what was there. */
export function RecordBalanceForm({ wallet }: RecordBalanceFormProps) {
  const { t } = useTranslation('investments');
  const toast = useToast();
  const mutation = useSnapshotMutation();
  const today = todayLocal();
  const [asOf, setAsOf] = useState(today);
  const [amount, setAmount] = useState('');
  const [note, setNote] = useState('');
  const [amountError, setAmountError] = useState<string | null>(null);
  const [dateError, setDateError] = useState<string | null>(null);

  const handleSubmit = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    const normalized = normalizeAmount(amount);
    const dateOk = asOf !== '' && asOf <= today;
    setAmountError(normalized === null ? t('amount_invalid') : null);
    setDateError(dateOk ? null : t('date_invalid'));
    if (normalized === null || !dateOk) return;
    mutation.mutate(
      { kind: 'record', walletId: wallet.id, asOf, amount: normalized, note: note.trim() || null },
      {
        onSuccess: () => {
          toast.success(t('balance_saved'));
          setAmount('');
          setNote('');
        },
      },
    );
  };

  return (
    <form className="space-y-4" noValidate onSubmit={handleSubmit} aria-labelledby="record-title">
      <h2 id="record-title" className="text-section font-semibold">
        {t('record_balance')}
      </h2>
      <div className="grid gap-4 sm:grid-cols-[auto_1fr]">
        <Field id="balance-date" label={t('date')} error={dateError ?? undefined}>
          <Input
            id="balance-date"
            type="date"
            max={today}
            value={asOf}
            aria-invalid={dateError !== null}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setAsOf(e.target.value);
            }}
          />
        </Field>
        <Field
          id="balance-amount"
          label={t('balance_in', { currency: wallet.currency })}
          error={amountError ?? undefined}
        >
          <Input
            id="balance-amount"
            inputMode="decimal"
            autoComplete="off"
            value={amount}
            aria-invalid={amountError !== null}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setAmount(e.target.value);
            }}
          />
        </Field>
      </div>
      <Field id="balance-note" label={t('note_optional')}>
        <Input
          id="balance-note"
          maxLength={NOTE_MAX}
          value={note}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
            setNote(e.target.value);
          }}
        />
      </Field>
      <p className="text-muted-foreground text-meta">
        {wallet.isArchived ? t('archived_balance_hint') : t('same_date_hint')}
      </p>
      {mutation.isError && <Banner variant="error">{t('balance_save_error')}</Banner>}
      <div className="flex justify-end">
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? t('saving') : t('save_balance')}
        </Button>
      </div>
    </form>
  );
}
